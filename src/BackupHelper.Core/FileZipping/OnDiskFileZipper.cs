using ICSharpCode.SharpZipLib.Zip;
using BackupHelper.Core.Sources;
using BackupHelper.Core.Utilities;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

public class OnDiskFileZipperFactory : IFileZipperFactory
{
    private readonly ILogger<OnDiskFileZipper> _logger;
    private readonly ISourceManager _sourceManager;

    public OnDiskFileZipperFactory(ILogger<OnDiskFileZipper> logger, ISourceManager sourceManager)
    {
        _logger = logger;
        _sourceManager = sourceManager;
    }

    public IFileZipper Create(string zipFilePath, bool overwriteFileIfExists, string? password)
    {
        return new OnDiskFileZipper(_logger, _sourceManager, zipFilePath, overwriteFileIfExists, password);
    }
}

public class OnDiskFileZipper : FileZipperBase
{
    private readonly ILogger<OnDiskFileZipper> _logger;
    private readonly ISourceManager _sourceManager;
    private readonly FileStream _zipFileStream;
    private readonly ZipOutputStream _zipOutputStream;

    public OnDiskFileZipper(ILogger<OnDiskFileZipper> logger,
                            ISourceManager sourceManager,
                            string zipFilePath,
                            bool overwriteFileIfExists,
                            string? password)
        : base(zipFilePath, overwriteFileIfExists)
    {
        _logger = logger;
        _sourceManager = sourceManager;

        var fileMode = overwriteFileIfExists ? FileMode.Create : FileMode.CreateNew;
        _zipFileStream = new FileStream(zipFilePath, fileMode, FileAccess.ReadWrite);
        _zipOutputStream = new ZipOutputStream(_zipFileStream);

        if (!string.IsNullOrEmpty(password))
        {
            _zipOutputStream.Password = password;
        }
        _zipOutputStream.SetLevel(9); // Maximum compression
    }

    public override bool HasToBeSaved => false;

    public override void AddFile(string filePath, string zipPath = "")
    {
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(filePath)).Replace('\\', '/');

        #if !DEBUG
        try
        {
        #endif
            var entry = new ZipEntry(newZipPath);
            var lastWriteTime = _sourceManager.GetFileLastWriteTime(filePath);

            if (lastWriteTime.HasValue)
            {
                entry.DateTime = lastWriteTime.Value;
            }

            _zipOutputStream.PutNextEntry(entry);

            using var fileStream = _sourceManager.GetStream(filePath);
            fileStream.CopyTo(_zipOutputStream);
            _zipOutputStream.CloseEntry();
        #if !DEBUG
        }
        catch (IOException e)
        {
            _logger.LogError("Failed to add file {FilePath} to zip file {ZipFileStreamName}: {ExMessage}", filePath, _zipFileStream.Name, e.Message);
        }
        #endif
    }

    public override void AddDirectory(string directoryPath, string zipPath = "")
    {
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(directoryPath)).Replace('\\', '/') + '/';

        var entry = new ZipEntry(newZipPath);
        var lastWriteTime = _sourceManager.GetDirectoryLastWriteTime(directoryPath);

        if (lastWriteTime.HasValue)
        {
            entry.DateTime = lastWriteTime.Value;
        }

        _zipOutputStream.PutNextEntry(entry);
        _zipOutputStream.CloseEntry();

        AddDirectoryContent(directoryPath, newZipPath.TrimEnd('/'));
    }

    public override void AddDirectoryContent(string directoryPath, string zipPath = "")
    {
        var subDirectories = _sourceManager.GetSubDirectories(directoryPath);
        var files = _sourceManager.GetFiles(directoryPath);

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
        _zipOutputStream.Dispose();
        _zipFileStream.Dispose();
    }
}