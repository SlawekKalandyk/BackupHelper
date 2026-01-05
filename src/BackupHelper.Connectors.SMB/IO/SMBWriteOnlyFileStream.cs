using SMBLibrary;
using SMBLibrary.Client;

namespace BackupHelper.Connectors.SMB.IO;

public class SMBWriteOnlyFileStream : Stream
{
    private readonly ISMBFileStore _fileStore;
    private readonly SMBFile _file;
    private readonly object _fileHandle;
    private long _position;
    private bool _disposed;

    public SMBWriteOnlyFileStream(ISMBFileStore fileStore, SMBFile file)
    {
        _fileStore = fileStore;
        _fileHandle = file.Handle;
        _position = 0;
    }

    public SMBWriteOnlyFileStream(ISMBFileStore fileStore, string filePath)
    {
        var file = SMBFile.OpenFileForWriting(fileStore, filePath);
        _file = file;
        _fileStore = fileStore;
        _fileHandle = file.Handle;
        _position = 0;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SMBWriteOnlyFileStream));

        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0 || offset + count > buffer.Length)
            throw new ArgumentOutOfRangeException();

        if (buffer.Length == 0 || count == 0)
            return;

        var data = new byte[count];
        Buffer.BlockCopy(buffer, offset, data, 0, count);

        var status = _fileStore.WriteFile(out var bytesWritten, _fileHandle, _position, data);

        if (status != NTStatus.STATUS_SUCCESS)
            throw new IOException($"SMB write failed: {status}");

        _position += bytesWritten;
    }

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override void Flush() { }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _file?.Dispose();
            _disposed = true;
        }
        base.Dispose(disposing);
    }
}
