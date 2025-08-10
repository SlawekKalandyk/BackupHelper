namespace BackupHelper.Core.FileZipping;

public abstract class FileZipperBase : IFileZipper
{
    protected FileZipperBase(string zipFilePath, bool overwriteFileIfExists)
    {
        ZipFilePath = zipFilePath;
        OverwriteFileIfExists = overwriteFileIfExists;
    }

    public bool EncryptHeaders { get; set; }
    public abstract bool HasToBeSaved { get; }
    public abstract bool CanEncryptHeaders { get; }
    protected string ZipFilePath { get; }
    protected bool OverwriteFileIfExists { get; }

    public abstract void AddFile(string filePath, string zipPath = "");
    public abstract void AddDirectory(string directoryPath, string zipPath = "");
    public abstract void AddDirectoryContent(string directoryPath, string zipPath = "");

    public void Save()
    {
        SaveCore();
    }

    protected virtual void SaveCore()
    {
        // Default implementation does nothing
    }

    public virtual void Dispose() { }
}