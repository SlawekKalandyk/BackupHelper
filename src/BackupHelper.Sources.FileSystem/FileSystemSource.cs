using System.Runtime.CompilerServices;
using BackupHelper.Sources.Abstractions;
using BackupHelper.Sources.FileSystem.FileInUseSource;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem;

public class FileSystemSource : ISource
{
    private readonly IFileInUseSourceManager _fileInUseSourceManager;
    private readonly ILogger<FileSystemSource> _logger;

    public FileSystemSource(
        IFileInUseSourceManager fileInUseSourceManager,
        ILogger<FileSystemSource> logger
    )
    {
        _fileInUseSourceManager = fileInUseSourceManager;
        _logger = logger;
    }

    public static string Scheme => "file";

    public string GetScheme() => Scheme;

    public Task<Stream> GetStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        return GetFromFileInUseSourceAsync(
            path,
            (p, _) =>
                Task.FromResult<Stream>(
                    new FileStream(
                        p,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        81920,
                        FileOptions.Asynchronous | FileOptions.SequentialScan
                    )
                ),
            (fileInUseSource, p, ct) => fileInUseSource.GetStreamAsync(p, ct),
            cancellationToken
        );
    }

    public Task<IEnumerable<string>> GetSubDirectoriesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return GetFromFileInUseSourceAsync(
            path,
            (p, _) => Task.FromResult<IEnumerable<string>>(Directory.GetDirectories(p)),
            (fileInUseSource, p, ct) => fileInUseSource.GetSubDirectoriesAsync(p, ct),
            cancellationToken
        );
    }

    public Task<IEnumerable<string>> GetFilesAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        return GetFromFileInUseSourceAsync(
            path,
            (p, _) => Task.FromResult<IEnumerable<string>>(Directory.GetFiles(p)),
            (fileInUseSource, p, ct) => fileInUseSource.GetFilesAsync(p, ct),
            cancellationToken
        );
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(path));
    }

    public Task<bool> DirectoryExistsAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Directory.Exists(path));
    }

    public Task<DateTime?> GetFileLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<DateTime?>(File.GetLastWriteTime(path));
    }

    public Task<DateTime?> GetDirectoryLastWriteTimeAsync(
        string path,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<DateTime?>(Directory.GetLastWriteTime(path));
    }

    public Task<long> GetFileSizeAsync(string path, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new FileInfo(path).Length);
    }

    private async Task<T> GetFromFileInUseSourceAsync<T>(
        string path,
        Func<string, CancellationToken, Task<T>> defaultFunc,
        Func<IFileInUseSource, string, CancellationToken, Task<T>> fileInUseFunc,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerName = ""
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            return await defaultFunc(path, cancellationToken);
        }
        catch (IOException)
        {
            try
            {
                var fileInUseSource = _fileInUseSourceManager.GetFileInUseSource(path);
                return await fileInUseFunc(fileInUseSource, path, cancellationToken);
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Error in {CallerName} for path: {Path}. Returning default value to preserve compatibility.",
                    callerName,
                    path
                );
                return default!;
            }
        }
    }

    public void Dispose()
    {
        _fileInUseSourceManager.Dispose();
    }
}
