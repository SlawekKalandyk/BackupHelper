namespace BackupHelper.Sources.Abstractions;

public interface ISource : IDisposable
{
    const string PrefixSeparator = "://";

    /// <summary>
    /// Returns the scheme of the source, e.g., "smb" or "file".
    /// </summary>
    string GetScheme();

    /// <summary>
    /// Returns the scheme prefix for the source, e.g., "smb://" or "file://".
    /// </summary>
    string GetSchemePrefix() => GetScheme() + PrefixSeparator;
    Stream GetStream(string path);
    IEnumerable<string> GetSubDirectories(string path);
    IEnumerable<string> GetFiles(string path);
    bool FileExists(string path);
    bool DirectoryExists(string path);
}