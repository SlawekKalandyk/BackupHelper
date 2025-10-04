using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

internal class ZipTaskQueue
{
    private readonly int _threadLimit;
    private readonly int _memoryLimitMb;
    private readonly ILogger<ZipTaskQueue> _logger;
    private readonly ConcurrentQueue<ZipTask> _tasks = new();
    private int _currentThreads = 0;
    private int _currentMemoryUsageMb = 0;
    private bool _isRunning = true;

    public ZipTaskQueue(int threadLimit, int memoryLimitMB, ILogger<ZipTaskQueue> logger)
    {
        _threadLimit = threadLimit;
        _memoryLimitMb = memoryLimitMB;
        _logger = logger;
        _logger.LogInformation(
            "ZipTaskQueue initialized with ThreadLimit: {ThreadLimit}, MemoryLimitMB: {MemoryLimitMB}",
            threadLimit,
            memoryLimitMB);
        Run();
    }

    public void Enqueue(ZipTask task)
    {
        _tasks.Enqueue(task);
    }

    public void Stop()
    {
        _isRunning = false;
    }

    private void Run()
    {
        Task.Run(
            async () =>
            {
                var executedTask = false;

                while (true)
                {
                    try
                    {
                        if (!_isRunning && _tasks.IsEmpty && _currentThreads == 0)
                            break;

                        if (_tasks.TryPeek(out var nextTask) &&
                            _currentThreads < _threadLimit &&
                            (_currentMemoryUsageMb + nextTask.FileSizeMB <= _memoryLimitMb || _currentThreads == 0) &&
                            _tasks.TryDequeue(out var taskToRun))
                        {
                            executedTask = true;
                            Interlocked.Increment(ref _currentThreads);
                            Interlocked.Add(ref _currentMemoryUsageMb, taskToRun.FileSizeMB);

                            _ = taskToRun.Task.ContinueWith(
                                t =>
                                {
                                    if (t.IsFaulted)
                                    {
                                        _logger?.LogError(t.Exception, "Task failed in ZipTaskQueue");
                                    }

                                    Interlocked.Decrement(ref _currentThreads);
                                    Interlocked.Add(ref _currentMemoryUsageMb, -taskToRun.FileSizeMB);
                                });

                            taskToRun.Task.Start();
                        }

                        if (_tasks.IsEmpty || !executedTask)
                            await Task.Delay(100); // Avoid busy waiting
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Error in ZipTaskQueue");
                    }
                }
            });
    }

    public void WaitForCompletion()
    {
        while (_currentThreads > 0 || !_tasks.IsEmpty)
        {
            Thread.Sleep(100);
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