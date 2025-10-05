namespace BackupHelper.Abstractions.ConnectionPooling;

public class PooledConnectionStream<TConnection, TEndpoint> : Stream
    where TEndpoint : notnull
{
    private readonly Stream _innerStream;
    private readonly TConnection _connection;
    private readonly TEndpoint _connectionPoolKey;
    private readonly ConnectionPoolBase<TConnection, TEndpoint> _connectionPoolBase;
    private bool _disposed;

    public PooledConnectionStream(Stream innerStream,
                                  TConnection connection,
                                  TEndpoint connectionPoolKey,
                                  ConnectionPoolBase<TConnection, TEndpoint> connectionPoolBase)
    {
        _innerStream = innerStream;
        _connection = connection;
        _connectionPoolKey = connectionPoolKey;
        _connectionPoolBase = connectionPoolBase;
    }

    public override bool CanRead => _innerStream.CanRead;
    public override bool CanSeek => _innerStream.CanSeek;
    public override bool CanWrite => _innerStream.CanWrite;
    public override long Length => _innerStream.Length;

    public override long Position
    {
        get => _innerStream.Position;
        set => _innerStream.Position = value;
    }

    public override void Flush()
        => _innerStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
        => _innerStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin)
        => _innerStream.Seek(offset, origin);

    public override void SetLength(long value)
        => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => _innerStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _connectionPoolBase.ReturnConnection(_connectionPoolKey, _connection);
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}