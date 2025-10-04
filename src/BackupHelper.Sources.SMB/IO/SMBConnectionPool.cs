using System.Collections.Concurrent;
using BackupHelper.Abstractions;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.SMB;

internal class SMBConnectionPool : IDisposable
{
    private readonly ICredentialsProvider _credentialsProvider;
    private readonly ILogger<SMBConnectionPool> _logger;
    private readonly ConcurrentDictionary<SMBShareInfo, BlockingCollection<ConnectionIdleWrapper>> _connectionPools = new();
    private readonly int _maxConnectionsPerShare;
    private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);
    private readonly TimeSpan _idleTimeout = TimeSpan.FromSeconds(90);
    private readonly CancellationTokenSource _cleanupCancellationSource = new CancellationTokenSource();
    private Task? _cleanupTask;

    public SMBConnectionPool(ICredentialsProvider credentialsProvider, ILogger<SMBConnectionPool> logger)
    {
        _credentialsProvider = credentialsProvider;
        _logger = logger;
        _maxConnectionsPerShare = 5;

        _cleanupTask = StartCleanupTask();
    }

    public SMBConnection GetConnection(SMBShareInfo shareInfo)
    {
        _poolLock.Wait();
        try
        {
            // Create the pool for this share if it doesn't exist
            if (!_connectionPools.TryGetValue(shareInfo, out var pool))
            {
                pool = new BlockingCollection<ConnectionIdleWrapper>(_maxConnectionsPerShare);
                _connectionPools[shareInfo] = pool;
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
                    wrapper.Connection.Dispose();
                }
            }

            // Create a new connection if needed
            var credential = GetCredential(shareInfo);
            return new SMBConnection(
                shareInfo.ServerIPAddress,
                string.Empty,
                shareInfo.ShareName,
                credential.Username,
                credential.Password
            );
        }
        finally
        {
            _poolLock.Release();
        }
    }

    public void ReturnConnection(SMBShareInfo shareInfo, SMBConnection connection)
    {
        _poolLock.Wait();
        try
        {
            if (_connectionPools.TryGetValue(shareInfo, out var pool) && pool.Count < _maxConnectionsPerShare && connection.IsConnected)
                pool.Add(new ConnectionIdleWrapper(connection));
            else
                connection.Dispose();
        }
        finally
        {
            _poolLock.Release();
        }
    }

    private SMBCredential GetCredential(SMBShareInfo shareInfo)
    {
        var credentialName = SMBCredentialHelper.GetSMBCredentialTitle(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName);
        var credential = _credentialsProvider.GetCredential(credentialName);

        if (credential == null)
            throw new InvalidOperationException($"No credentials found for SMB share '{credentialName}'.");

        return new SMBCredential(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName, credential.Username, credential.Password!);
    }

    private bool ValidateConnection(SMBConnection connection)
    {
        try
        {
            return connection.IsConnected && connection.TestConnection();
        }
        catch
        {
            return false;
        }
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
                    _logger?.LogError(ex, "Error occurred during SMB connection pool cleanup.");
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

            foreach (var shareInfo in _connectionPools.Keys)
            {
                if (_connectionPools.TryGetValue(shareInfo, out var pool))
                {
                    var tempPool = new List<ConnectionIdleWrapper>();

                    while (pool.TryTake(out var wrapper))
                    {
                        if (now - wrapper.LastUsed <= _idleTimeout)
                            tempPool.Add(wrapper);
                        else
                            wrapper.Connection.Dispose();
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
                wrapper.Connection.Dispose();
            }
        }
        
        _poolLock.Dispose();
        _cleanupCancellationSource.Dispose();
    }

    /// <summary>
    /// Wrapper over SMBConnection to track last used time for idle timeout management.
    /// </summary>
    private class ConnectionIdleWrapper
    {
        public SMBConnection Connection { get; }
        public DateTime LastUsed { get; set; }

        public ConnectionIdleWrapper(SMBConnection connection)
        {
            Connection = connection;
            LastUsed = DateTime.UtcNow;
        }
    }
}