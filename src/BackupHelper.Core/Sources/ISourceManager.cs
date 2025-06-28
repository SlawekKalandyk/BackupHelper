namespace BackupHelper.Core.Sources;

public interface ISourceManager
{
    Stream GetStream(string path);
    IEnumerable<string> GetSubDirectories(string path);
    IEnumerable<string> GetFiles(string path);
}