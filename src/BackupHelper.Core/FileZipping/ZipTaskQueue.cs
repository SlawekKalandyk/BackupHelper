using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

internal class ZipTaskQueue
{
    private readonly int _threadLimit;
    private readonly int _memoryLimitMb;
    private readonly ILogger<ZipTaskQueue> _logger;
    private readonly ConcurrentBag<string> _failedFiles;
    private readonly ConcurrentQueue<ZipTask> _tasks = new();
    private readonly SemaphoreSlim _workSignal = new(0);
    private readonly Task _runnerTask;
    private readonly ConcurrentDictionary<int, Task> _runningTasks = new();
    private int _currentThreads = 0;
    private int _currentMemoryUsageMb = 0;
    private bool _isRunning = true;

    public ZipTaskQueue(
        int threadLimit,
        int memoryLimitMB,
        ILogger<ZipTaskQueue> logger,
        ConcurrentBag<string> failedFiles
    )
    {
        _threadLimit = threadLimit;
        _memoryLimitMb = memoryLimitMB;
        _logger = logger;
        _failedFiles = failedFiles;
        _logger.LogInformation(
            "ZipTaskQueue initialized with ThreadLimit: {ThreadLimit}, MemoryLimitMB: {MemoryLimitMB}",
            threadLimit,
            memoryLimitMB
        );
        _runnerTask = RunAsync();
    }

    public Task EnqueueAsync(ZipTask task, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _tasks.Enqueue(task);
        _workSignal.Release();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _isRunning = false;
        _workSignal.Release();
        await _runnerTask;
    }

    private async Task RunAsync()
    {
        while (true)
        {
            try
            {
                if (!_isRunning && _tasks.IsEmpty && _currentThreads == 0)
                    break;

                var executedTask = false;

                while (
                    _tasks.TryPeek(out var nextTask)
                    && _currentThreads < _threadLimit
                    && HasSufficientMemoryFor(nextTask)
                    && _tasks.TryDequeue(out var taskToRun)
                )
                {
                    executedTask = true;
                    Interlocked.Increment(ref _currentThreads);
                    Interlocked.Add(ref _currentMemoryUsageMb, taskToRun.FileSizeMB);

                    var runningTask = ExecuteTaskAsync(taskToRun);
                    _runningTasks.TryAdd(runningTask.Id, runningTask);
                }

                if (!executedTask)
                {
                    await _workSignal.WaitAsync(TimeSpan.FromMilliseconds(100));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ZipTaskQueue");
            }
        }
    }

    private async Task ExecuteTaskAsync(ZipTask taskToRun)
    {
        try
        {
            await taskToRun.Work(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _failedFiles.Add(taskToRun.FilePath);
            _logger.LogError(ex, "Task failed in ZipTaskQueue for {FilePath}", taskToRun.FilePath);
        }
        finally
        {
            _runningTasks.TryRemove(Task.CurrentId ?? -1, out _);
            Interlocked.Decrement(ref _currentThreads);
            Interlocked.Add(ref _currentMemoryUsageMb, -taskToRun.FileSizeMB);
            _workSignal.Release();
        }
    }

    private bool HasSufficientMemoryFor(ZipTask nextTask)
    {
        if (_memoryLimitMb <= 0)
        {
            return true;
        }

        return _currentMemoryUsageMb + nextTask.FileSizeMB <= _memoryLimitMb
            || _currentThreads == 0;
    }

    public async Task WaitForCompletionAsync(CancellationToken cancellationToken = default)
    {
        while (_currentThreads > 0 || !_tasks.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _workSignal.WaitAsync(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        await Task.WhenAll(_runningTasks.Values);
    }
}

internal class ZipTask
{
    public int FileSizeMB { get; }
    public Func<CancellationToken, Task> Work { get; }
    public string FilePath { get; }

    public ZipTask(int fileSizeMb, Func<CancellationToken, Task> work, string filePath)
    {
        FileSizeMB = fileSizeMb;
        Work = work;
        FilePath = filePath;
    }
}