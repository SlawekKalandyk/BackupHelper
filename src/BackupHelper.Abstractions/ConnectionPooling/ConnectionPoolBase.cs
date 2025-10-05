using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Abstractions.ConnectionPooling;

/// <summary>
/// Abstract connection pool for managing connections to various endpoints
/// </summary>
/// <typeparam name="TConnection">The type of connection to manage</typeparam>
/// <typeparam name="TEndpoint">The type of endpoint identifier</typeparam>
public abstract class ConnectionPoolBase<TConnection, TEndpoint> : IDisposable
    where TEndpoint : notnull
{
    protected readonly ILogger _logger;

    private readonly ConcurrentDictionary<TEndpoint, ConcurrentBag<ConnectionIdleWrapper>> _connectionPools = new();
    private readonly int _maxConnectionsPerEndpoint;
    private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
    private readonly TimeSpan _idleTimeout;
    private readonly CancellationTokenSource _cleanupCancellationSource = new CancellationTokenSource();
    private Task? _cleanupTask;

    protected ConnectionPoolBase(ILogger logger, int maxConnectionsPerEndpoint, TimeSpan idleTimeout)
    {
        _logger = logger;
        _maxConnectionsPerEndpoint = maxConnectionsPerEndpoint;
        _idleTimeout = idleTimeout;
        _cleanupTask = StartCleanupTask();
    }

    public TConnection GetConnection(TEndpoint endpoint)
    {
        _poolLock.Wait();

        try
        {
            // Create the pool for this endpoint if it doesn't exist
            if (!_connectionPools.TryGetValue(endpoint, out var pool))
            {
                pool = new ConcurrentBag<ConnectionIdleWrapper>();
                _connectionPools[endpoint] = pool;
            }

            // Try to get an existing connection from pool
            if (pool.TryTake(out var wrapper))
            {
                if (ValidateConnection(wrapper.Connection))
                {
                    wrapper.LastUsed = DateTime.UtcNow;

                    return wrapper.Connection;
                }
                else
                {
                    DisposeConnection(wrapper.Connection);
                }
            }

            // Create a new connection if needed
            return CreateConnection(endpoint);
        }
        finally
        {
            _poolLock.Release();
        }
    }

    public void ReturnConnection(TEndpoint endpoint, TConnection connection)
    {
        _poolLock.Wait();

        try
        {
            if (_connectionPools.TryGetValue(endpoint, out var pool) &&
                pool.Count < _maxConnectionsPerEndpoint &&
                ValidateConnection(connection))
            {
                pool.Add(new ConnectionIdleWrapper(connection));
            }
            else
            {
                DisposeConnection(connection);
            }
        }
        finally
        {
            _poolLock.Release();
        }
    }

    protected abstract TConnection CreateConnection(TEndpoint endpoint);
    protected abstract void DisposeConnection(TConnection connection);

    protected virtual bool ValidateConnection(TConnection connection)
    {
        return true;
    }

    private Task StartCleanupTask()
    {
        return Task.Run(
            async () =>
            {
                try
                {
                    while (!_cleanupCancellationSource.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30), _cleanupCancellationSource.Token);

                        if (_cleanupCancellationSource.IsCancellationRequested)
                            break;

                        await CleanupIdleConnections();
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during connection pool cleanup.");

                    // Restart the cleanup task in case of unexpected errors
                    _cleanupTask = StartCleanupTask();
                }
            });
    }

    private async Task CleanupIdleConnections()
    {
        await _poolLock.WaitAsync();

        try
        {
            var now = DateTime.UtcNow;

            foreach (var endpoint in _connectionPools.Keys)
            {
                if (_connectionPools.TryGetValue(endpoint, out var pool))
                {
                    var tempPool = new List<ConnectionIdleWrapper>();

                    while (pool.TryTake(out var wrapper))
                    {
                        if (now - wrapper.LastUsed <= _idleTimeout)
                            tempPool.Add(wrapper);
                        else
                            DisposeConnection(wrapper.Connection);
                    }

                    foreach (var connection in tempPool)
                    {
                        pool.Add(connection);
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

        foreach (var pool in _connectionPools.Values)
        {
            while (pool.TryTake(out var wrapper))
            {
                DisposeConnection(wrapper.Connection);
            }
        }

        _poolLock.Dispose();
        _cleanupCancellationSource.Dispose();
    }

    /// <summary>
    /// Wrapper over connection to track last used time for idle timeout management.
    /// </summary>
    private class ConnectionIdleWrapper
    {
        public TConnection Connection { get; }
        public DateTime LastUsed { get; set; }

        public ConnectionIdleWrapper(TConnection connection)
        {
            Connection = connection;
            LastUsed = DateTime.UtcNow;
        }
    }
}