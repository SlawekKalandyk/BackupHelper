namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public interface IFileInUseSourceFactory
{
    IFileInUseSource Create();
}

public interface IFileInUseSource : IDisposable
{
    Stream GetStream(string path);
    IEnumerable<string> GetSubDirectories(string path);
    IEnumerable<string> GetFiles(string path);
}
