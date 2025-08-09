using System.IO.Compression;
using BackupHelper.Sources.FileSystem.Vss;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public class VssFileInUseSourceFactory : IFileInUseSourceFactory
{
    private readonly ILogger<VssBackup> _logger;

    public VssFileInUseSourceFactory(ILogger<VssBackup> logger)
    {
        _logger = logger;
    }

    public IFileInUseSource Create()
    {
        return new VssFileInUseSource(_logger);
    }
}

public class VssFileInUseSource : IFileInUseSource
{
    private readonly ILogger<VssBackup> _logger;
    private readonly IDictionary<string, VssBackup> _vssBackups = new Dictionary<string, VssBackup>();

    public VssFileInUseSource(ILogger<VssBackup> logger)
    {
        _logger = logger;
    }

    public void AddEntry(ZipArchive zipArchive, string filePath, string zipPath, CompressionLevel compressionLevel)
    {
        var snapshotPath = GetSnapshotPath(filePath);
        zipArchive.CreateEntryFromFile(snapshotPath, zipPath, compressionLevel);
    }

    public Stream GetStream(string path)
    {
        var snapshotPath = GetSnapshotPath(path);
        return new FileStream(snapshotPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public IEnumerable<string> GetSubDirectories(string path)
    {
        var snapshotPath = GetSnapshotPath(path);
        return Directory.GetDirectories(snapshotPath);
    }

    public IEnumerable<string> GetFiles(string path)
    {
        var snapshotPath = GetSnapshotPath(path);
        return Directory.GetFiles(snapshotPath);
    }

    private string GetSnapshotPath(string path)
    {
        var volume = Path.GetPathRoot(path);
        if (!_vssBackups.TryGetValue(volume, out var vssBackup))
        {
            vssBackup = new VssBackup(_logger);
            _vssBackups[volume] = vssBackup;
        }

        vssBackup.Setup(volume);
        return vssBackup.GetSnapshotPath(path);
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