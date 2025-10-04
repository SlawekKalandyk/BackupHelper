using System;
using System.IO;

namespace BackupHelper.Sources.SMB;

internal class PooledSMBStream : Stream
{
    private readonly Stream _innerStream;
    private readonly SMBConnection _connection;
    private readonly SMBShareInfo _shareInfo;
    private readonly SMBConnectionPool _connectionPool;
    private bool _disposed;

    public PooledSMBStream(Stream innerStream, SMBConnection connection, SMBShareInfo shareInfo, SMBConnectionPool connectionPool)
    {
        _innerStream = innerStream;
        _connection = connection;
        _shareInfo = shareInfo;
        _connectionPool = connectionPool;
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
                _connectionPool.ReturnConnection(_shareInfo, _connection);
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}