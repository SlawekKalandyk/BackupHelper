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

    public void Enqueue(ZipTask task)
    {
        _tasks.Enqueue(task);
        _workSignal.Release();
    }

    public void Stop()
    {
        _isRunning = false;
        _workSignal.Release();
        _runnerTask.GetAwaiter().GetResult();
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

                    _ = taskToRun.Task.ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            _failedFiles.Add(taskToRun.FilePath);
                            _logger.LogError(
                                t.Exception,
                                "Task failed in ZipTaskQueue for {FilePath}",
                                taskToRun.FilePath
                            );
                        }

                        Interlocked.Decrement(ref _currentThreads);
                        Interlocked.Add(ref _currentMemoryUsageMb, -taskToRun.FileSizeMB);
                        _workSignal.Release();
                    });

                    taskToRun.Task.Start();
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

    private bool HasSufficientMemoryFor(ZipTask nextTask)
    {
        if (_memoryLimitMb <= 0)
        {
            return true;
        }

        return _currentMemoryUsageMb + nextTask.FileSizeMB <= _memoryLimitMb
            || _currentThreads == 0;
    }

    public void WaitForCompletion()
    {
        while (_currentThreads > 0 || !_tasks.IsEmpty)
        {
            _workSignal.Wait(TimeSpan.FromMilliseconds(100));
        }
    }
}

internal class ZipTask
{
    public int FileSizeMB { get; }
    public Task Task { get; }
    public string FilePath { get; }

    public ZipTask(int fileSizeMb, Task task, string filePath)
    {
        FileSizeMB = fileSizeMb;
        Task = task;
        FilePath = filePath;
    }
}