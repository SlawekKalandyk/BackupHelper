using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;
using BackupHelper.Core.Sources;
using BackupHelper.Core.Utilities;

namespace BackupHelper.Core.FileZipping;

public class InMemoryFileZipperFactory : IFileZipperFactory
{
    private readonly ILogger<InMemoryFileZipper> _logger;
    private readonly ISourceManager _sourceManager;

    public InMemoryFileZipperFactory(ILogger<InMemoryFileZipper> logger, ISourceManager sourceManager)
    {
        _logger = logger;
        _sourceManager = sourceManager;
    }

    public IFileZipper Create(string zipFilePath, bool overwriteFileIfExists, string? password)
    {
        return new InMemoryFileZipper(_logger, _sourceManager, zipFilePath, overwriteFileIfExists, password);
    }
}

public class InMemoryFileZipper : FileZipperBase
{
    private readonly ILogger<InMemoryFileZipper> _logger;
    private readonly ISourceManager _sourceManager;
    private readonly MemoryStream _zipMemoryStream;
    private readonly ZipOutputStream _zipOutputStream;
    private readonly bool _encrypt;

    public InMemoryFileZipper(ILogger<InMemoryFileZipper> logger,
                              ISourceManager sourceManager,
                              string zipFilePath,
                              bool overwriteFileIfExists,
                              string? password)
        : base(zipFilePath, overwriteFileIfExists)
    {
        _logger = logger;
        _sourceManager = sourceManager;
        _zipMemoryStream = new MemoryStream();
        _zipOutputStream = new ZipOutputStream(_zipMemoryStream);

        if (!string.IsNullOrEmpty(password))
        {
            _zipOutputStream.Password = password;
            _encrypt = true;
        }

        _zipOutputStream.SetLevel(9); // Maximum compression
    }

    public override bool HasToBeSaved => true;
    public override bool CanEncryptHeaders => false;

    protected override void SaveCore()
    {
        if (OverwriteFileIfExists && File.Exists(ZipFilePath))
        {
            _logger.LogWarning("Overwriting existing file: {ZipFilePath}", ZipFilePath);
            File.Delete(ZipFilePath);
        }

        _zipOutputStream.Finish();
        using var fileStream = File.Open(ZipFilePath, FileMode.Create, FileAccess.ReadWrite);
        _zipMemoryStream.Seek(0, SeekOrigin.Begin);
        _zipMemoryStream.CopyTo(fileStream);
    }

    public override void AddFile(string filePath, string zipPath = "", int? compressionLevel = null)
    {
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(filePath)).Replace('\\', '/');
        try
        {
            var entry = new ZipEntry(newZipPath)
            {
                DateTime = File.GetLastWriteTime(filePath),
                AESKeySize = _encrypt ? 256 : 0
            };

            _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);

            _zipOutputStream.PutNextEntry(entry);

            using var fileStream = _sourceManager.GetStream(filePath);
            fileStream.CopyTo(_zipOutputStream);
            _zipOutputStream.CloseEntry();
        }
        catch (Exception e)
        {
            _logger.LogError("Failed to add file {FilePath} to zip file: {ExMessage}", filePath, e.Message);
        }
    }

    public override void AddDirectory(string directoryPath, string zipPath = "", int? compressionLevel = null)
    {
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(directoryPath)).Replace('\\', '/') + '/';

        var entry = new ZipEntry(newZipPath)
        {
            DateTime = Directory.GetLastWriteTime(directoryPath),
            AESKeySize = _encrypt ? 256 : 0
        };

        _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);

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

    public override void Dispose()
    {
        _zipOutputStream.Dispose();
        _zipMemoryStream.Dispose();
    }
}