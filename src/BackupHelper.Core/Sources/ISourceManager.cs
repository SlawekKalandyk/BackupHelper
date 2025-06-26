namespace BackupHelper.Core.Sources;

public interface ISourceManager
{
    Stream GetStream(string path);
}