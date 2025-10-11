using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Abstractions.ResourcePooling;

/// <summary>
/// Abstract resource pool for managing resources like connections.
/// Implements pooling, idle timeout management, and thread safety.
/// </summary>
/// <typeparam name="TResource">The type of resource to manage</typeparam>
/// <typeparam name="TResourceId">The type of resource identifier</typeparam>
public abstract class ResourcePoolBase<TResource, TResourceId> : IDisposable
    where TResourceId : notnull
{
    protected readonly ILogger _logger;

    private readonly ConcurrentDictionary<
        TResourceId,
        ConcurrentBag<ResourceIdleWrapper>
    > _resourcePools = new();
    private readonly int _maxResourcesPerIdentifier;
    private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
    private readonly TimeSpan _idleTimeout;
    private readonly CancellationTokenSource _cleanupCancellationSource =
        new CancellationTokenSource();
    private Task? _cleanupTask;

    protected ResourcePoolBase(ILogger logger, int maxResourcesPerIdentifier, TimeSpan idleTimeout)
    {
        _logger = logger;
        _maxResourcesPerIdentifier = maxResourcesPerIdentifier;
        _idleTimeout = idleTimeout;
        _cleanupTask = StartCleanupTask();
    }

    public TResource GetResource(TResourceId resourceId)
    {
        _poolLock.Wait();

        try
        {
            // Create the pool for this identifier if it doesn't exist
            if (!_resourcePools.TryGetValue(resourceId, out var pool))
            {
                pool = new ConcurrentBag<ResourceIdleWrapper>();
                _resourcePools[resourceId] = pool;
            }

            // Try to get an existing resource from pool
            if (pool.TryTake(out var wrapper))
            {
                if (ValidateResource(wrapper.Resource))
                {
                    wrapper.LastUsed = DateTime.UtcNow;

                    return wrapper.Resource;
                }
                else
                {
                    DisposeResource(wrapper.Resource);
                }
            }

            // Create a new resource if needed
            return CreateResource(resourceId);
        }
        finally
        {
            _poolLock.Release();
        }
    }

    public void ReturnResource(TResourceId resourceId, TResource resource)
    {
        _poolLock.Wait();

        try
        {
            if (
                _resourcePools.TryGetValue(resourceId, out var pool)
                && pool.Count < _maxResourcesPerIdentifier
                && ValidateResource(resource)
            )
            {
                pool.Add(new ResourceIdleWrapper(resource));
            }
            else
            {
                DisposeResource(resource);
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    protected abstract TResource CreateResource(TResourceId resourceId);
    protected abstract void DisposeResource(TResource resource);

    protected virtual bool ValidateResource(TResource resource)
    {
        return true;
    }

    private Task StartCleanupTask()
    {
        return Task.Run(async () =>
        {
            try
            {
                while (!_cleanupCancellationSource.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), _cleanupCancellationSource.Token);

                    if (_cleanupCancellationSource.IsCancellationRequested)
                        break;

                    await CleanupIdleResources();
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during resource pool cleanup.");

                // Restart the cleanup task in case of unexpected errors
                _cleanupTask = StartCleanupTask();
            }
        });
    }

    private async Task CleanupIdleResources()
    {
        await _poolLock.WaitAsync();

        try
        {
            var now = DateTime.UtcNow;

            foreach (var resourceId in _resourcePools.Keys)
            {
                if (_resourcePools.TryGetValue(resourceId, out var pool))
                {
                    var tempPool = new List<ResourceIdleWrapper>();

                    while (pool.TryTake(out var wrapper))
                    {
                        if (now - wrapper.LastUsed <= _idleTimeout)
                            tempPool.Add(wrapper);
                        else
                            DisposeResource(wrapper.Resource);
                    }

                    foreach (var resource in tempPool)
                    {
                        pool.Add(resource);
                    }
                }
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    public void Dispose()
    {
        _cleanupCancellationSource.Cancel();
        _cleanupTask?.Wait();

        foreach (var pool in _resourcePools.Values)
        {
            while (pool.TryTake(out var wrapper))
            {
                DisposeResource(wrapper.Resource);
            }
        }

        _poolLock.Dispose();
        _cleanupCancellationSource.Dispose();
    }

    /// <summary>
    /// Wrapper over resource to track last used time for idle timeout management.
    /// </summary>
    private class ResourceIdleWrapper
    {
        public TResource Resource { get; }
        public DateTime LastUsed { get; set; }

        public ResourceIdleWrapper(TResource resource)
        {
            Resource = resource;
            LastUsed = DateTime.UtcNow;
        }
    }
}
