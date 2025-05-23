using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.FileZipping;

public class BackupPlanZipper : IBackupPlanZipper
{
    private readonly ILogger<BackupPlanZipper> _logger;
    private readonly IFileZipperFactory _fileZipperFactory;

    public BackupPlanZipper(ILogger<BackupPlanZipper> logger, IFileZipperFactory fileZipperFactory)
    {
        _logger = logger;
        _fileZipperFactory = fileZipperFactory;
    }

    public void CreateZipFile(BackupPlan plan, string outputPath)
    {
        _logger.LogInformation("Creating backup file at {OutputPath}", outputPath);

        using var fileZipper = _fileZipperFactory.Create();
        foreach (var entry in plan.Items)
        {
            AddEntryToZip(fileZipper, entry, string.Empty);
        }

        if (fileZipper.HasToBeSaved)
        {
            _logger.LogInformation("Saving zip to {OutputPath}", outputPath);
            fileZipper.Save(outputPath, overwrite: true);
        }
    }

    private void AddEntryToZip(IFileZipper zipper, BackupEntry entry, string zipPath)
    {
        switch (entry)
        {
            case BackupFileEntry fileEntry:
                AddBackupFileEntryToZip(zipper, fileEntry, zipPath);
                break;
            case BackupDirectoryEntry dirEntry:
                AddBackupDirectoryEntryToZip(zipper, dirEntry, zipPath);
                break;
        }
    }

    private void AddBackupFileEntryToZip(IFileZipper zipper, BackupFileEntry fileEntry, string zipPath)
    {
        if (File.Exists(fileEntry.FilePath))
        {
            _logger.LogInformation("Adding file to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                                   fileEntry.FilePath, string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/');
            zipper.AddFile(fileEntry.FilePath, zipPath);
        }
        else if (Directory.Exists(fileEntry.FilePath))
        {
            _logger.LogInformation("Adding directory to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                                   fileEntry.FilePath, string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/');
            zipper.AddDirectory(fileEntry.FilePath, zipPath);
        }
        else
        {
            _logger.LogWarning("File or directory not found: {FileEntryFilePath}", fileEntry.FilePath);
        }
    }

    private void AddBackupDirectoryEntryToZip(IFileZipper zipper, BackupDirectoryEntry dirEntry, string zipPath)
    {
        var newZipPath = string.IsNullOrEmpty(zipPath) ? dirEntry.DirectoryName : Path.Combine(zipPath, dirEntry.DirectoryName);
        foreach (var subEntry in dirEntry.Items)
        {
            AddEntryToZip(zipper, subEntry, newZipPath);
        }
    }
}