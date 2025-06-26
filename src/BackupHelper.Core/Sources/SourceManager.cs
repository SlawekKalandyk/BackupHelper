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
        var scheme = GetScheme(path);

        if (string.IsNullOrEmpty(scheme))
        {
            if (!_sources.TryGetValue(DefaultScheme, out var defaultSource))
            {
                throw new NotSupportedException("No default file source is registered.");
            }
            return defaultSource.GetStream(path);
        }

        var prefix = scheme + "://";
        var source = GetSource(scheme);
        var pathWithoutScheme = path[prefix.Length..];
        return source.GetStream(pathWithoutScheme);
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
        var idx = filePath.IndexOf("://", StringComparison.Ordinal);
        return idx < 0 ? string.Empty : filePath[..idx];
    }
}