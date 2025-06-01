using System.IO.Compression;
using BackupHelper.Core.Vss;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileInUseZipEntryHandler;

public class VssFileInUseZipEntryHandlerFactory : IFileInUseZipEntryHandlerFactory
{
    private readonly ILogger<VssBackup> _logger;

    public VssFileInUseZipEntryHandlerFactory(ILogger<VssBackup> logger)
    {
        _logger = logger;
    }

    public IFileInUseZipEntryHandler Create()
    {
        return new VssFileInUseZipEntryHandler(_logger);
    }
}

internal class VssFileInUseZipEntryHandler : IFileInUseZipEntryHandler
{
    private readonly ILogger<VssBackup> _logger;
    private readonly IDictionary<string, VssBackup> _vssBackups = new Dictionary<string, VssBackup>();

    public VssFileInUseZipEntryHandler(ILogger<VssBackup> logger)
    {
        _logger = logger;
    }

    public void AddEntry(ZipArchive zipArchive, string filePath, string zipPath, CompressionLevel compressionLevel)
    {
        var volume = Path.GetPathRoot(filePath);

        if (!_vssBackups.TryGetValue(volume, out var vssBackup))
        {
            vssBackup = new VssBackup(_logger);
            _vssBackups[volume] = vssBackup;
        }

        vssBackup.Setup(volume);
        var snapshotPath = vssBackup.GetSnapshotPath(filePath);

        zipArchive.CreateEntryFromFile(snapshotPath, zipPath, compressionLevel);
    }

    public void Dispose()
    {
        foreach (var vssBackup in _vssBackups.Values)
        {
            vssBackup.Dispose();
        }
        _vssBackups.Clear();
    }
}