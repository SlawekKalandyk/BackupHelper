using System.Collections.Concurrent;
using BackupHelper.Abstractions;

namespace BackupHelper.Sources.SMB;

public class SMBConnectionPool : IDisposable
{
    private readonly ICredentialsProvider _credentialsProvider;
    private readonly ConcurrentDictionary<SMBShareInfo, BlockingCollection<SMBConnection>> _connectionPools = new();
    private readonly int _maxConnectionsPerShare;
    private readonly SemaphoreSlim _poolLock = new SemaphoreSlim(1, 1);

    public SMBConnectionPool(ICredentialsProvider credentialsProvider, int maxConnectionsPerShare = 5)
    {
        _credentialsProvider = credentialsProvider;
        _maxConnectionsPerShare = maxConnectionsPerShare;
    }

    public SMBConnection GetConnection(SMBShareInfo shareInfo)
    {
        _poolLock.Wait();
        try
        {
            // Create the pool for this share if it doesn't exist
            if (!_connectionPools.TryGetValue(shareInfo, out var pool))
            {
                pool = new BlockingCollection<SMBConnection>(_maxConnectionsPerShare);
                _connectionPools[shareInfo] = pool;
            }

            // Try to get an existing connection from pool
            if (pool.TryTake(out var connection) && connection.IsConnected)
            {
                return connection;
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
            if (_connectionPools.TryGetValue(shareInfo, out var pool) && pool.Count < _maxConnectionsPerShare)
            {
                pool.Add(connection);
            }
            else
            {
                connection.Dispose();
            }
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
        {
            throw new InvalidOperationException($"No credentials found for SMB share '{credentialName}'.");
        }

        return new SMBCredential(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName, credential.Username, credential.Password!);
    }

    public void Dispose()
    {
        foreach (var pool in _connectionPools.Values)
        {
            while (pool.TryTake(out var connection))
            {
                connection.Dispose();
            }
        }
        _poolLock.Dispose();
    }
}