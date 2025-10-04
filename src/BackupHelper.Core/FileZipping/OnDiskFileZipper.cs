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

    public IFileZipper Create(string zipFilePath, bool overwriteFileIfExists, string? password)
    {
        return new OnDiskFileZipper(_loggerFactory, _sourceManager, zipFilePath, overwriteFileIfExists, password);
    }
}

public class OnDiskFileZipper : FileZipperBase
{
    private readonly Lock _zipFileEntryLock = new();
    private readonly ILogger<OnDiskFileZipper> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ISourceManager _sourceManager;
    private readonly string? _password;
    private readonly FileStream _zipFileStream;
    private readonly ZipOutputStream _zipOutputStream;
    private readonly bool _encrypt;
    private ZipTaskQueue? _zipTaskQueue;

    public OnDiskFileZipper(ILoggerFactory loggerFactory,
                            ISourceManager sourceManager,
                            string zipFilePath,
                            bool overwriteFileIfExists,
                            string? password)
        : base(zipFilePath, overwriteFileIfExists)
    {
        _logger = loggerFactory.CreateLogger<OnDiskFileZipper>();
        _loggerFactory = loggerFactory;
        _sourceManager = sourceManager;
        _password = password;

        var fileMode = overwriteFileIfExists ? FileMode.Create : FileMode.CreateNew;
        _zipFileStream = new FileStream(zipFilePath, fileMode, FileAccess.ReadWrite);
        _zipOutputStream = new ZipOutputStream(_zipFileStream);

        if (!string.IsNullOrEmpty(password))
        {
            _zipOutputStream.Password = password;
            _encrypt = true;
        }
    }

    public override bool HasToBeSaved => false;

    private ZipTaskQueue ZipTaskQueue
    {
        get
        {
            _zipTaskQueue ??= new ZipTaskQueue(ThreadLimit, MemoryLimitMB, _loggerFactory.CreateLogger<ZipTaskQueue>());
            return _zipTaskQueue;
        }
    }

    public override void AddFile(string filePath, string zipPath = "", int? compressionLevel = null)
    {
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(filePath)).Replace('\\', '/');

    #if !DEBUG
        try
        {
    #endif
        var entry = new ZipEntry(newZipPath)
        {
            AESKeySize = _encrypt ? 256 : 0
        };
        var lastWriteTime = _sourceManager.GetFileLastWriteTime(filePath);

        if (lastWriteTime.HasValue)
        {
            entry.DateTime = lastWriteTime.Value;
        }

        if (ThreadLimit <= 1)
        {
            AddEntryToZipSynchronously(entry, filePath, compressionLevel);
        }
        else
        {
            var fileSizeMB = (int)Math.Ceiling((double)_sourceManager.GetFileSize(filePath) / (1024 * 1024));
            var zipTask = fileSizeMB < MemoryLimitMB
                              ? new ZipTask(
                                  fileSizeMB,
                                  new Task(() => AddEntryToZipInParallel(entry, filePath, compressionLevel), TaskCreationOptions.None),
                                  filePath)
                              : new ZipTask(
                                  fileSizeMB,
                                  new Task(() => AddEntryToZipSynchronously(entry, filePath, compressionLevel), TaskCreationOptions.None),
                                  filePath);
            ZipTaskQueue.Enqueue(zipTask);
        }
    #if !DEBUG
        }
        catch (IOException e)
        {
            _logger.LogError("Failed to add file {FilePath} to zip file {ZipFileStreamName}: {ExMessage}", filePath, _zipFileStream.Name, e.Message);
        }
    #endif
    }

    public override void AddDirectory(string directoryPath, string zipPath = "", int? compressionLevel = null)
    {
        var newZipPath = Path.Combine(zipPath, PathHelper.GetName(directoryPath)).Replace('\\', '/') + '/';

        var entry = new ZipEntry(newZipPath)
        {
            AESKeySize = _encrypt ? 256 : 0
        };
        var lastWriteTime = _sourceManager.GetDirectoryLastWriteTime(directoryPath);

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

    public override void Wait()
    {
        ZipTaskQueue.WaitForCompletion();
    }

    private void AddEntryToZipInParallel(ZipEntry entry, string filePath, int? compressionLevel)
    {
        var deflater = new Deflater(compressionLevel ?? DefaultCompressionLevel, true);
        var crc32 = new Crc32();
        long size = 0;
        using var outputMemoryStream = new MemoryStream();
        using var zipStream = new DeflaterOutputStream(outputMemoryStream, deflater);

        using (var fileStream = _sourceManager.GetStream(filePath))
        {
            var buffer = new byte[8192];
            int bytesRead;

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                crc32.Update(new ArraySegment<byte>(buffer, 0, bytesRead));
                zipStream.Write(buffer, 0, bytesRead);
                size += bytesRead;
            }

            zipStream.Finish();
        }

        entry.CompressionMethod = CompressionMethod.Deflated;
        entry.Size = size;
        entry.CompressedSize = outputMemoryStream.Length;
        entry.Crc = crc32.Value;

        outputMemoryStream.Position = 0;

        lock (_zipFileEntryLock)
        {
            _zipOutputStream.PutNextPassthroughEntry(entry);
            outputMemoryStream.CopyTo(_zipOutputStream);
            _zipOutputStream.CloseEntry();
        }
    }

    private void AddEntryToZipSynchronously(ZipEntry entry, string filePath, int? compressionLevel)
    {
        lock (_zipFileEntryLock)
        {
            _zipOutputStream.SetLevel(compressionLevel ?? DefaultCompressionLevel);
            _zipOutputStream.PutNextEntry(entry);

            using var fileStream = _sourceManager.GetStream(filePath);
            fileStream.CopyTo(_zipOutputStream);
            _zipOutputStream.CloseEntry();
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        ZipTaskQueue.Stop();
        _zipOutputStream.Dispose();
        _zipFileStream.Dispose();
    }
}