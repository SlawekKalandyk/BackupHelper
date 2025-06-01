using System.IO.Compression;
using BackupHelper.Core.FileInUseZipEntryHandler;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

public class OnDiskFileZipperFactory : IFileZipperFactory
{
    private readonly ILogger<OnDiskFileZipper> _logger;
    private readonly IFileInUseZipEntryHandlerManager _fileInUseZipEntryHandlerManager;

    public OnDiskFileZipperFactory(ILogger<OnDiskFileZipper> logger, IFileInUseZipEntryHandlerManager fileInUseZipEntryHandlerManager)
    {
        _logger = logger;
        _fileInUseZipEntryHandlerManager = fileInUseZipEntryHandlerManager;
    }

    public IFileZipper Create(string zipFilePath, bool overwriteFileIfExists)
    {
        return new OnDiskFileZipper(_logger, _fileInUseZipEntryHandlerManager, zipFilePath, overwriteFileIfExists);
    }
}

public class OnDiskFileZipper : FileZipperBase
{
    private readonly ILogger<OnDiskFileZipper> _logger;
    private readonly IFileInUseZipEntryHandlerManager _fileInUseZipEntryHandlerManager;
    private readonly FileStream _zipFileStream;
    private readonly ZipArchive _zipArchive;

    public OnDiskFileZipper(ILogger<OnDiskFileZipper> logger,
                            IFileInUseZipEntryHandlerManager fileInUseZipEntryHandlerManager,
                            string zipFilePath,
                            bool overwriteFileIfExists)
        : base(zipFilePath, overwriteFileIfExists)
    {
        _logger = logger;
        _fileInUseZipEntryHandlerManager = fileInUseZipEntryHandlerManager;

        var fileMode = overwriteFileIfExists ? FileMode.Create : FileMode.CreateNew;
        _zipFileStream = new FileStream(zipFilePath, fileMode, FileAccess.ReadWrite);
        _zipArchive = new ZipArchive(_zipFileStream, ZipArchiveMode.Create, false);
    }

    public override bool HasToBeSaved => false;

    public override void AddFile(string filePath, string zipPath = "")
    {
        var fileInfo = new FileInfo(filePath);
        var newZipPath = Path.Combine(zipPath, fileInfo.Name);

        try
        {
            _zipArchive.CreateEntryFromFile(filePath, newZipPath, CompressionLevel.Optimal);
        }
        catch (IOException)
        {
#if !DEBUG
            try
            {
#endif
            var fileInUseZipEntryHandler = _fileInUseZipEntryHandlerManager.GetFileInUseZipEntryHandler(filePath);
            fileInUseZipEntryHandler.AddEntry(_zipArchive, filePath, newZipPath, CompressionLevel.Optimal);
#if !DEBUG
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to add file {FilePath} to zip file {ZipFileStreamName}: {ExMessage}", filePath, _zipFileStream.Name, e.Message);
            }
#endif
        }
    }

    public override void AddDirectory(string directoryPath, string zipPath = "")
    {
        var directoryInfo = new DirectoryInfo(directoryPath);
        var newZipPath = Path.Combine(zipPath, directoryInfo.Name);

        _zipArchive.CreateEntry(newZipPath + '/');
        AddDirectoryContent(directoryPath, newZipPath);
    }

    public override void AddDirectoryContent(string directoryPath, string zipPath = "")
    {
        var subDirectories = Directory.GetDirectories(directoryPath);
        var files = Directory.GetFiles(directoryPath);

        foreach (var subDirectoryPath in subDirectories)
        {
            AddDirectory(subDirectoryPath, zipPath);
        }

        foreach (var filePath in files)
        {
            AddFile(filePath, zipPath);
        }
    }

    public override void Save()
    {
        throw new NotSupportedException();
    }

    public override void Dispose()
    {
        base.Dispose();
        _zipArchive.Dispose();
        _zipFileStream.Dispose();
    }
}