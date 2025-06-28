namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public interface IFileInUseSourceFactory
{
    IFileInUseSource Create();
}

public interface IFileInUseSource : IDisposable
{
    Stream GetStream(string path);
}