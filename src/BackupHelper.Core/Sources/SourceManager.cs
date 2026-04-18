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

    public Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            (source, pathWithoutScheme, ct) => source.GetStreamAsync(pathWithoutScheme, ct),
            cancellationToken
        );
    }

    public Task<IEnumerable<string>> GetSubDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            async (source, pathWithoutScheme, ct) =>
            {
                var scheme = GetScheme(path);
                var directories = await source.GetSubDirectoriesAsync(pathWithoutScheme, ct);

                return string.IsNullOrEmpty(scheme)
                    ? directories
                    : directories.Select(dir => Path.Join(scheme + ISource.PrefixSeparator, dir));
            },
            cancellationToken
        );
    }

    public Task<IEnumerable<string>> GetFilesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            async (source, pathWithoutScheme, ct) =>
            {
                var scheme = GetScheme(path);
                var files = await source.GetFilesAsync(pathWithoutScheme, ct);

                return string.IsNullOrEmpty(scheme)
                    ? files
                    : files.Select(file => Path.Join(scheme + ISource.PrefixSeparator, file));
            },
            cancellationToken
        );
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            (source, pathWithoutScheme, ct) => source.FileExistsAsync(pathWithoutScheme, ct),
            cancellationToken
        );
    }

    public Task<bool> DirectoryExistsAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            (source, pathWithoutScheme, ct) => source.DirectoryExistsAsync(pathWithoutScheme, ct),
            cancellationToken
        );
    }

    public Task<DateTime?> GetFileLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            (source, pathWithoutScheme, ct) =>
                source.GetFileLastWriteTimeAsync(pathWithoutScheme, ct),
            cancellationToken
        );
    }

    public Task<DateTime?> GetDirectoryLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            (source, pathWithoutScheme, ct) =>
                source.GetDirectoryLastWriteTimeAsync(pathWithoutScheme, ct),
            cancellationToken
        );
    }

    public Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return GetFromSourceAsync(
            path,
            (source, pathWithoutScheme, ct) => source.GetFileSizeAsync(pathWithoutScheme, ct),
            cancellationToken
        );
    }

    private Task<T> GetFromSourceAsync<T>(
        string path,
        Func<ISource, string, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken
    )
    {
        var scheme = GetScheme(path);

        if (string.IsNullOrEmpty(scheme))
        {
            if (!_sources.TryGetValue(DefaultScheme, out var defaultSource))
            {
                throw new NotSupportedException("No default file source is registered.");
            }

            return action(defaultSource, path, cancellationToken);
        }

        var source = GetSource(scheme);
        var prefix = source.GetSchemePrefix();
        var pathWithoutScheme = path[prefix.Length..];

        return action(source, pathWithoutScheme, cancellationToken);
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
