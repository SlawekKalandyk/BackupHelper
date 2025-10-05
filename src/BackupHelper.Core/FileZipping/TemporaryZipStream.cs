using BackupHelper.Core.Utilities;

namespace BackupHelper.Core.FileZipping;

/// <summary>
/// A stream that uses a MemoryStream for files smaller than or equal to 1GB,
/// and a temporary file on disk for larger files.
/// </summary>
internal class TemporaryZipStream : Stream
{
    private const int ThresholdFileSizeMB = 1024;
    private readonly MemoryStream? _memoryStream;
    private readonly TemporaryFile? _temporaryFile;
    private readonly FileStream? _fileStream;

    public TemporaryZipStream(int fileSizeMB)
    {
        if (fileSizeMB <= ThresholdFileSizeMB)
        {
            _memoryStream = new MemoryStream();
        }
        else
        {
            _temporaryFile = new TemporaryFile();
            _fileStream = new FileStream(_temporaryFile.FilePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        }
    }

    private Stream ActiveStream => _memoryStream ?? (Stream)_fileStream!;
    public override bool CanRead => ActiveStream.CanRead;
    public override bool CanSeek => ActiveStream.CanSeek;
    public override bool CanWrite => ActiveStream.CanWrite;
    public override long Length => ActiveStream.Length;

    public override long Position
    {
        get => ActiveStream.Position;
        set => ActiveStream.Position = value;
    }

    public override void Flush()
        => ActiveStream.Flush();

    public override int Read(byte[] buffer, int offset, int count)
        => ActiveStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin)
        => ActiveStream.Seek(offset, origin);

    public override void SetLength(long value)
        => ActiveStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count)
        => ActiveStream.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _memoryStream?.Dispose();
            _fileStream?.Dispose();
            _temporaryFile?.Dispose();
        }
        base.Dispose(disposing);
    }
}