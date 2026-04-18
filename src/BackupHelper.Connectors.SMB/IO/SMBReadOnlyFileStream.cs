using System.Buffers;
using SMBLibrary;
using SMBLibrary.Client;

namespace BackupHelper.Connectors.SMB.IO;

public class SMBReadOnlyFileStream : Stream
{
    private readonly SMBFile? _ownedFile;
    private readonly ISMBFileStore _fileStore;
    private readonly object _fileHandle;
    private readonly long _length;
    private long _position;
    private bool _disposed;

    public SMBReadOnlyFileStream(ISMBFileStore fileStore, SMBFile file)
    {
        _ownedFile = null;
        _fileStore = fileStore;
        _fileHandle = file.Handle;
        _length = file.FileInfo.StandardInformation.EndOfFile;
        _position = 0;
    }

    public SMBReadOnlyFileStream(ISMBFileStore fileStore, string filePath)
    {
        var file = SMBFile.OpenFileForReading(fileStore, filePath);
        _ownedFile = file;
        _fileStore = fileStore;
        _fileHandle = file.Handle;
        _length = file.FileInfo.StandardInformation.EndOfFile;
        _position = 0;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SMBReadOnlyFileStream));

        var status = _fileStore.ReadFile(out var data, _fileHandle, _position, count);

        if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
            throw new IOException($"SMB read failed: {status}");

        if (data == null)
            return 0;

        var bytesRead = data.Length;
        Array.Copy(data, 0, buffer, offset, bytesRead);
        _position += bytesRead;
        return bytesRead;
    }

    public override Task<int> ReadAsync(
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken
    )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<int>(cancellationToken);
        }

        return Task.FromResult(Read(buffer, offset, count));
    }

    public override ValueTask<int> ReadAsync(
        Memory<byte> buffer,
        CancellationToken cancellationToken = default
    )
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<int>(cancellationToken);
        }

        if (buffer.Length == 0)
        {
            return ValueTask.FromResult(0);
        }

        var rentedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
        try
        {
            var bytesRead = Read(rentedBuffer, 0, buffer.Length);
            rentedBuffer.AsSpan(0, bytesRead).CopyTo(buffer.Span);
            return ValueTask.FromResult(bytesRead);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _ownedFile?.Dispose();
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}