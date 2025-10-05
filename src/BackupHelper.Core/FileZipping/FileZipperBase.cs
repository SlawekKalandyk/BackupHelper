using System.Collections.Concurrent;

namespace BackupHelper.Core.FileZipping;

public abstract class FileZipperBase : IFileZipper
{
    protected readonly ConcurrentBag<string> _failedFiles = new();

    protected FileZipperBase(string zipFilePath, bool overwriteFileIfExists)
    {
        ZipFilePath = zipFilePath;
        OverwriteFileIfExists = overwriteFileIfExists;
    }

    public int ThreadLimit { get; set; } = 1;
    public int MemoryLimitMB { get; set; } = 0;
    public IReadOnlyCollection<string> FailedFiles => _failedFiles;
    public virtual int DefaultCompressionLevel => 9;
    public abstract bool HasToBeSaved { get; }
    protected string ZipFilePath { get; }
    protected bool OverwriteFileIfExists { get; }

    public abstract void AddFile(string filePath, string zipPath = "", int? compressionLevel = null);
    public abstract void AddDirectory(string directoryPath, string zipPath = "", int? compressionLevel = null);
    public abstract void AddDirectoryContent(string directoryPath, string zipPath = "", int? compressionLevel = null);

    public void Save()
    {
        SaveCore();
    }

    public virtual void Wait()
    {
        // Default implementation does nothing
    }

    protected virtual void SaveCore()
    {
        // Default implementation does nothing
    }

    public virtual void Dispose() { }
}