namespace BackupHelper.Core.FileZipping;

public abstract class FileZipperBase : IFileZipper
{
    protected FileZipperBase(string zipFilePath, bool overwriteFileIfExists)
    {
        ZipFilePath = zipFilePath;
        OverwriteFileIfExists = overwriteFileIfExists;
    }

    public abstract bool HasToBeSaved { get; }

    protected string ZipFilePath { get; }
    protected bool OverwriteFileIfExists { get; }

    public abstract void AddFile(string filePath, string zipPath = "");
    public abstract void AddDirectory(string directoryPath, string zipPath = "");
    public abstract void AddDirectoryContent(string directoryPath, string zipPath = "");
    public abstract void Save();

    public virtual void Dispose() { }
}