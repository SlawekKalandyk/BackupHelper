using BackupHelper.Sources.Abstractions;
using BackupHelper.Sources.FileSystem;

namespace BackupHelper.Core.Sources;

public class SourceManager : ISourceManager
{
    private readonly Dictionary<string, ISource> _sources;
    private readonly string DefaultScheme = FileSystemSource.Scheme;

    public SourceManager(IEnumerable<ISource> sources)
    {
        _sources = sources.ToDictionary(s => s.GetScheme());
    }

    public Stream GetStream(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.GetStream(pathWithoutScheme));
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.GetSubDirectories(pathWithoutScheme));
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.GetFiles(pathWithoutScheme));
    }

    public bool FileExists(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.FileExists(pathWithoutScheme));
    }

    public bool DirectoryExists(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.DirectoryExists(pathWithoutScheme));
    }

    public DateTime? GetFileLastWriteTime(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.GetFileLastWriteTime(pathWithoutScheme));
    }

    public DateTime? GetDirectoryLastWriteTime(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.GetDirectoryLastWriteTime(pathWithoutScheme));
    }

    private T GetFromSource<T>(string path, Func<ISource, string, T> action)
    {
        var scheme = GetScheme(path);

        if (string.IsNullOrEmpty(scheme))
        {
            if (!_sources.TryGetValue(DefaultScheme, out var defaultSource))
            {
                throw new NotSupportedException("No default file source is registered.");
            }

            return action(defaultSource, path);
        }

        var source = GetSource(scheme);
        var prefix = source.GetSchemePrefix();
        var pathWithoutScheme = path[prefix.Length..];

        return action(source, pathWithoutScheme);
    }


    private ISource GetSource(string scheme)
    {
        if (_sources.TryGetValue(scheme, out var source))
        {
            return source;
        }

        throw new NotSupportedException($"Source with scheme '{scheme}' is not supported.");
    }

    private string GetScheme(string filePath)
    {
        var idx = filePath.IndexOf(ISource.PrefixSeparator, StringComparison.Ordinal);
        return idx < 0 ? string.Empty : filePath[..idx];
    }
}