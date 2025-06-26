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
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (IOException)
        {
        #if !DEBUG
            try
            {
        #endif
            var fileInUseSource = _fileInUseSourceManager.GetFileInUseSource(path);
            return fileInUseSource.GetStream(path);
        #if !DEBUG
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to get file {FilePath}: {ExMessage}", path, e.Message);
            }
        #endif
        }
    }
}