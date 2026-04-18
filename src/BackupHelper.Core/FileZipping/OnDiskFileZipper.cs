using BackupHelper.Abstractions.Credentials;
using BackupHelper.Core.Sources;
using BackupHelper.Core.Utilities;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

public class OnDiskFileZipperFactory : IFileZipperFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISourceManager _sourceManager;

    public OnDiskFileZipperFactory(ILoggerFactory loggerFactory, ISourceManager sourceManager)
    {
        _loggerFactory = loggerFactory;
        _sourceManager = sourceManager;
    }

    public IFileZipper Create(
        string zipFilePath,
        bool overwriteFileIfExists,
        SensitiveString? password
    )
    {
        return new OnDiskFileZipper(
            _loggerFactory,
            _sourceManager,
            zipFilePath,
            overwriteFileIfExists,
            password
        );
    }
}

public class OnDiskFileZipper : FileZipperBase
{
    private readonly Lock _zipFileEntryLock = new();
    private readonly ILogger<OnDiskFileZipper> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISourceManager _sourceManager;
    private readonly FileStream _zipFileStream;
    private readonly ZipOutputStream _zipOutputStream;
    private readonly bool _encrypt;
    private ZipTaskQueue? _zipTaskQueue;

    public OnDiskFileZipper(
        ILoggerFactory loggerFactory,
        ISourceManager sourceManager,
        string zipFilePath,
        bool overwriteFileIfExists,
        SensitiveString? password
    )
        : base(zipFilePath, overwriteFileIfExists)
    {
        _logger = loggerFactory.CreateLogger<OnDiskFileZipper>();
        _loggerFactory = loggerFactory;
        _sourceManager = sourceManager;

        var fileMode = overwriteFileIfExists ? FileMode.Create : FileMode.CreateNew;
        _zipFileStream = new FileStream(zipFilePath, fileMode, FileAccess.ReadWrite);
        _zipOutputStream = new ZipOutputStream(_zipFileStream);
        _zipOutputStream.UseZip64 = UseZip64.On;

        if (password is not null && !password.IsEmpty)
        {
            _zipOutputStream.Password = password.Expose();
            _encrypt = true;
        }
    }

    public override bool HasToBeSaved => false;

    private ZipTaskQueue ZipTaskQueue
    {
        get
        {
            _zipTaskQueue ??= new ZipTaskQueue(
                ThreadLimit,
                MemoryLimitMB,
                _loggerFactory.CreateLogger<ZipTaskQueue>(),
                _failedFiles
            );
            return _zipTaskQueue;
        }
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

#if !DEBUG
        try
        {
#endif
            var entry = new ZipEntry(newZipPath) { AESKeySize = _encrypt ? 256 : 0 };
            var lastWriteTime = await _sourceManager.GetFileLastWriteTimeAsync(
                filePath,
                cancellationToken
            );

            if (lastWriteTime.HasValue)
            {
                entry.DateTime = lastWriteTime.Value;
            }

            if (ThreadLimit <= 1)
            {
                try
                {
                    await AddEntryToZipSynchronouslyAsync(
                        entry,
                        filePath,
                        compressionLevel,
                        cancellationToken
                    );
                }
                catch
                {
                    _failedFiles.Add(filePath);
                    throw;
                }
            }
            else
            {
                var fileSizeBytes = await _sourceManager.GetFileSizeAsync(filePath, cancellationToken);
                var fileSizeMB = (int)Math.Ceiling((double)fileSizeBytes / (1024 * 1024));
                var zipTask =
                    MemoryLimitMB <= 0 || fileSizeMB < MemoryLimitMB
                        ? new ZipTask(
                            fileSizeMB,
                            ct =>
                                AddEntryToZipInParallelAsync(
                                    entry,
                                    filePath,
                                    fileSizeMB,
                                    compressionLevel,
                                    ct
                                ),
                            filePath
                        )
                        : new ZipTask(
                            fileSizeMB,
                            ct =>
                                AddEntryToZipSynchronouslyAsync(
                                    entry,
                                    filePath,
                                    compressionLevel,
                                    ct
                                ),
                            filePath
                        );

                await ZipTaskQueue.EnqueueAsync(zipTask, cancellationToken);
            }
#if !DEBUG
        }
        catch (IOException e)
        {
            _logger.LogError(
                "Failed to add file {FilePath} to zip file {ZipFileStreamName}: {ExMessage}",
                filePath,
                _zipFileStream.Name,
                e.Message
            );
        }
#endif
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

        var entry = new ZipEntry(newZipPath) { AESKeySize = _encrypt ? 256 : 0 };
        var lastWriteTime = await _sourceManager.GetDirectoryLastWriteTimeAsync(
            directoryPath,
            cancellationToken
        );

        if (lastWriteTime.HasValue)
        {
            entry.DateTime = lastWriteTime.Value;
        }

        lock (_zipFileEntryLock)
        {
            _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);
            _zipOutputStream.PutNextEntry(entry);
            _zipOutputStream.CloseEntry();
        }

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

    public override Task SaveAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public override Task WaitAsync(CancellationToken cancellationToken = default)
    {
        return ZipTaskQueue.WaitForCompletionAsync(cancellationToken);
    }

    private async Task AddEntryToZipInParallelAsync(
        ZipEntry entry,
        string filePath,
        int fileSizeMB,
        int? compressionLevel,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var deflater = new Deflater(compressionLevel ?? DefaultCompressionLevel, true);
        var crc32 = new Crc32();
        long accurateSize = 0;

        await using var outputStream = new TemporaryZipStream(fileSizeMB);
        await using var zipStream = new DeflaterOutputStream(outputStream, deflater);

        await using (var fileStream = await _sourceManager.GetStreamAsync(filePath, cancellationToken))
        {
            var buffer = new byte[8192];
            int bytesRead;

            while (
                (bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken))
                > 0
            )
            {
                crc32.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
                await zipStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                accurateSize += bytesRead;
            }

            zipStream.Finish();
        }

        entry.CompressionMethod = CompressionMethod.Deflated;
        entry.Size = accurateSize;
        entry.CompressedSize = outputStream.Length;
        entry.Crc = crc32.Value;

        outputStream.Position = 0;

        lock (_zipFileEntryLock)
        {
            _zipOutputStream.PutNextPassthroughEntry(entry);
            outputStream.CopyTo(_zipOutputStream);
            _zipOutputStream.CloseEntry();
        }
    }

    private async Task AddEntryToZipSynchronouslyAsync(
        ZipEntry entry,
        string filePath,
        int? compressionLevel,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var fileStream = await _sourceManager.GetStreamAsync(filePath, cancellationToken);

        lock (_zipFileEntryLock)
        {
            _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);
            _zipOutputStream.PutNextEntry(entry);

            fileStream.CopyTo(_zipOutputStream);
            _zipOutputStream.CloseEntry();
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_zipTaskQueue != null)
        {
            await _zipTaskQueue.StopAsync();
            await _zipTaskQueue.WaitForCompletionAsync();
        }

        _zipOutputStream.Dispose();
        await _zipFileStream.DisposeAsync();

        await base.DisposeAsync();
    }
}