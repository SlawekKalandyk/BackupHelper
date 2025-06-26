namespace BackupHelper.Sources.Abstractions;

public interface ISource
{
    string GetScheme();
    Stream GetStream(string path);
}