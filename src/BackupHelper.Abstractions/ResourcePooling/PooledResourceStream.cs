namespace BackupHelper.Abstractions.ResourcePooling;

public class PooledResourceStream<TResource, TResourceId> : Stream
    where TResourceId : notnull
{
    private readonly Stream _innerStream;
    private readonly TResource _resource;
    private readonly TResourceId _resourceId;
    private readonly ResourcePoolBase<TResource, TResourceId> _resourcePoolBase;
    private bool _disposed;

    public PooledResourceStream(
        Stream innerStream,
        TResource resource,
        TResourceId resourceId,
        ResourcePoolBase<TResource, TResourceId> resourcePoolBase
    )
    {
        _innerStream = innerStream;
        _resource = resource;
        _resourceId = resourceId;
        _resourcePoolBase = resourcePoolBase;
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

    public override void Flush() => _innerStream.Flush();

    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _innerStream.FlushAsync(cancellationToken);

    public override int Read(byte[] buffer, int offset, int count) =>
        _innerStream.Read(buffer, offset, count);

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => _innerStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    ) => _innerStream.ReadAsync(buffer, cancellationToken);

    public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);

    public override void SetLength(long value) => _innerStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) =>
        _innerStream.Write(buffer, offset, count);

    public override Task WriteAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    ) => _innerStream.WriteAsync(buffer, offset, count, cancellationToken);

    public override ValueTask WriteAsync(
        ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = default
    ) => _innerStream.WriteAsync(buffer, cancellationToken);

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _innerStream.Dispose();
                _resourcePoolBase.ReturnResource(_resourceId, _resource);
            }
            _disposed = true;
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _innerStream.DisposeAsync();
            _resourcePoolBase.ReturnResource(_resourceId, _resource);
            _disposed = true;
        }

        await base.DisposeAsync();
    }
}
