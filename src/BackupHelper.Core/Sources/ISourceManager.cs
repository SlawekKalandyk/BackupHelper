namespace BackupHelper.Core.Sources;

public interface ISourceManager
{
    Stream GetStream(string path);
    IEnumerable<string> GetSubDirectories(string path);
    IEnumerable<string> GetFiles(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
    DateTime? GetFileLastWriteTime(string path);
    DateTime? GetDirectoryLastWriteTime(string directoryPath);
    long GetFileSize(string path);
}
