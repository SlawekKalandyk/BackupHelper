using System.Runtime.CompilerServices;
using BackupHelper.Sources.Abstractions;
using BackupHelper.Sources.FileSystem.FileInUseSource;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem;

public class FileSystemSource : ISource
{
    private readonly IFileInUseSourceManager _fileInUseSourceManager;
    private readonly ILogger<FileSystemSource> _logger;

    public FileSystemSource(IFileInUseSourceManager fileInUseSourceManager, ILogger<FileSystemSource> logger)
    {
        _fileInUseSourceManager = fileInUseSourceManager;
        _logger = logger;
    }

    public static string Scheme => "file";

    public string GetScheme() => Scheme;

    public Stream GetStream(string path)
    {
        return GetFromFileInUseSource(
            path,
            (p) => new FileStream(p, FileMode.Open, FileAccess.Read, FileShare.Read),
            (fileInUseSource, p) => fileInUseSource.GetStream(p));
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        return GetFromFileInUseSource(
            path,
            (p) => Directory.GetDirectories(p),
            (fileInUseSource, p) => fileInUseSource.GetSubDirectories(p));
    }

    public IEnumerable<string> GetFiles(string path)
    {
        return GetFromFileInUseSource(
            path,
            (p) => Directory.GetFiles(p),
            (fileInUseSource, p) => fileInUseSource.GetFiles(p));
    }

    public bool FileExists(string path)
        => File.Exists(path);

    public bool DirectoryExists(string path)
        => Directory.Exists(path);

    public DateTime? GetFileLastWriteTime(string path)
        => File.GetLastWriteTime(path);

    public DateTime? GetDirectoryLastWriteTime(string path)
        => Directory.GetLastWriteTime(path);

    public long GetFileSize(string path)
        => new FileInfo(path).Length;

    private T GetFromFileInUseSource<T>(string path, Func<string, T> defaultFunc, Func<IFileInUseSource, string, T> fileInUseFunc,
                                        [CallerMemberName] string callerName = "")
    {
        try
        {
            return defaultFunc(path);
        }
        catch (IOException)
        {
            #if !DEBUG
            try
            {
            #endif
                var fileInUseSource = _fileInUseSourceManager.GetFileInUseSource(path);
                return fileInUseFunc(fileInUseSource, path);
            #if !DEBUG
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error in {CallerName} for path: {Path}", callerName, path);
                return default;
            }
            #endif
        }
    }

    public void Dispose()
    {

    }
}