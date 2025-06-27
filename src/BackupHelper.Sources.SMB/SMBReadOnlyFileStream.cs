using SMBLibrary;
using SMBLibrary.Client;

namespace BackupHelper.Sources.SMB;

public class SMBReadOnlyFileStream : Stream
{
    private readonly SMBFile? _file;
    private readonly ISMBFileStore _fileStore;
    private readonly object _fileHandle;
    private readonly long _length;
    private long _position;
    private bool _disposed;

    public SMBReadOnlyFileStream(ISMBFileStore fileStore, SMBFile file)
    {
        _fileStore = fileStore;
        _fileHandle = file.Handle;
        _length = file.FileInfo.StandardInformation.EndOfFile;
        _position = 0;
    }

    public SMBReadOnlyFileStream(ISMBFileStore fileStore, string filePath)
    {
        var file = SMBFile.OpenFileForReading(fileStore, filePath);
        _file = file;
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

    public override long Seek(long offset, SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

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