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
        return GetFromSource(path, (source, pathWithoutScheme) =>
                                   {
                                       var scheme = GetScheme(path);
                                       return string.IsNullOrEmpty(scheme)
                                           ? source.GetSubDirectories(pathWithoutScheme)
                                           : source.GetSubDirectories(pathWithoutScheme)
                                                   .Select(dir => Path.Join(scheme + ISource.PrefixSeparator, dir));
                                   });
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) =>
                                   {
                                       var scheme = GetScheme(path);
                                       return string.IsNullOrEmpty(scheme)
                                                  ? source.GetFiles(pathWithoutScheme)
                                                  : source.GetFiles(pathWithoutScheme)
                                                          .Select(dir => Path.Join(scheme + ISource.PrefixSeparator, dir));
                                   });
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

    public long GetFileSize(string path)
    {
        return GetFromSource(path, (source, pathWithoutScheme) => source.GetFileSize(pathWithoutScheme));
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