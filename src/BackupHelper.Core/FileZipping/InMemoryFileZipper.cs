using BackupHelper.Abstractions.Credentials;
using BackupHelper.Core.Sources;
using BackupHelper.Core.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

public class InMemoryFileZipperFactory : IFileZipperFactory
{
    private readonly ILogger<InMemoryFileZipper> _logger;
    private readonly ISourceManager _sourceManager;

    public InMemoryFileZipperFactory(
        ILogger<InMemoryFileZipper> logger,
        ISourceManager sourceManager
    )
    {
        _logger = logger;
        _sourceManager = sourceManager;
    }

    public IFileZipper Create(
        string zipFilePath,
        bool overwriteFileIfExists,
        SensitiveString? password
    )
    {
        return new InMemoryFileZipper(
            _logger,
            _sourceManager,
            zipFilePath,
            overwriteFileIfExists,
            password
        );
    }
}

public class InMemoryFileZipper : FileZipperBase
{
    private readonly ILogger<InMemoryFileZipper> _logger;
    private readonly ISourceManager _sourceManager;
    private readonly MemoryStream _zipMemoryStream;
    private readonly ZipOutputStream _zipOutputStream;
    private readonly bool _encrypt;

    public InMemoryFileZipper(
        ILogger<InMemoryFileZipper> logger,
        ISourceManager sourceManager,
        string zipFilePath,
        bool overwriteFileIfExists,
        SensitiveString? password
    )
        : base(zipFilePath, overwriteFileIfExists)
    {
        _logger = logger;
        _sourceManager = sourceManager;
        _zipMemoryStream = new MemoryStream();
        _zipOutputStream = new ZipOutputStream(_zipMemoryStream);

        if (password is not null && !password.IsEmpty)
        {
            _zipOutputStream.Password = password.Expose();
            _encrypt = true;
        }

        _zipOutputStream.SetLevel(9); // Maximum compression
    }

    public override bool HasToBeSaved => true;

    public override async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (OverwriteFileIfExists && File.Exists(ZipFilePath))
        {
            _logger.LogWarning("Overwriting existing file: {ZipFilePath}", ZipFilePath);
            File.Delete(ZipFilePath);
        }

        _zipOutputStream.Finish();
        await using var fileStream = File.Open(
            ZipFilePath,
            FileMode.Create,
            FileAccess.ReadWrite,
            FileShare.None
        );
        _zipMemoryStream.Seek(0, SeekOrigin.Begin);
        await _zipMemoryStream.CopyToAsync(fileStream, cancellationToken);
    }

    public override async Task AddFileAsync(
        string filePath,
        string zipPath = "",
        int? compressionLevel = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(filePath)).Replace('\\', '/');

        try
        {
            var lastWriteTime = await _sourceManager.GetFileLastWriteTimeAsync(
                filePath,
                cancellationToken
            );
            var entry = new ZipEntry(newZipPath)
            {
                DateTime = lastWriteTime ?? DateTime.Now,
                AESKeySize = _encrypt ? 256 : 0,
            };

            _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);

            _zipOutputStream.PutNextEntry(entry);

            await using var fileStream = await _sourceManager.GetStreamAsync(filePath, cancellationToken);
            await fileStream.CopyToAsync(_zipOutputStream, cancellationToken);
            _zipOutputStream.CloseEntry();
        }
        catch (Exception e)
        {
            _logger.LogError(
                "Failed to add file {FilePath} to zip file: {ExMessage}",
                filePath,
                e.Message
            );
        }
    }

    public override async Task AddDirectoryAsync(
        string directoryPath,
        string zipPath = "",
        int? compressionLevel = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newZipPath =
            Path.Combine(zipPath, PathHelper.GetName(directoryPath)).Replace('\\', '/') + '/';

        var lastWriteTime = await _sourceManager.GetDirectoryLastWriteTimeAsync(
            directoryPath,
            cancellationToken
        );
        var entry = new ZipEntry(newZipPath)
        {
            DateTime = lastWriteTime ?? DateTime.Now,
            AESKeySize = _encrypt ? 256 : 0,
        };

        _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);

        _zipOutputStream.PutNextEntry(entry);
        _zipOutputStream.CloseEntry();

        await AddDirectoryContentAsync(
            directoryPath,
            newZipPath.TrimEnd('/'),
            compressionLevel,
            cancellationToken
        );
    }

    public override async Task AddDirectoryContentAsync(
        string directoryPath,
        string zipPath = "",
        int? compressionLevel = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var subDirectories = await _sourceManager.GetSubDirectoriesAsync(
            directoryPath,
            cancellationToken
        );
        var files = await _sourceManager.GetFilesAsync(directoryPath, cancellationToken);

        foreach (var subDirectoryPath in subDirectories)
        {
            await AddDirectoryAsync(subDirectoryPath, zipPath, compressionLevel, cancellationToken);
        }

        foreach (var filePath in files)
        {
            await AddFileAsync(filePath, zipPath, compressionLevel, cancellationToken);
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _zipOutputStream.Dispose();
        await _zipMemoryStream.DisposeAsync();

        await base.DisposeAsync();
    }
}