namespace BackupHelper.Core.FileZipping
{
    public interface IFileZipper : IDisposable
    {
        bool HasToBeSaved { get; }
        void AddFile(string filePath, string zipPath);
        void AddDirectory(string directoryPath, string zipPath);
        void AddDirectoryContent(string directoryPath, string zipPath);
        void Save(string filePath, bool overwrite);
    }
}
