using BackupHelper.Abstractions;
using BackupHelper.Core.FileZipping;
using BackupHelper.Core.Sources;
using BackupHelper.Core.Utilities;
using ICSharpCode.SharpZipLib.Zip;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.BackupZipping;

public class BackupPlanZipper : IBackupPlanZipper
{
    private readonly ILogger<BackupPlanZipper> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IDateTimeProvider _dateTimeProvider;

    public BackupPlanZipper(ILogger<BackupPlanZipper> logger, IServiceScopeFactory serviceScopeFactory, IDateTimeProvider dateTimeProvider)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _dateTimeProvider = dateTimeProvider;
    }

    public void CreateZipFile(BackupPlan plan, string outputPath, string? password = null)
    {
        _logger.LogInformation("Creating backup file at {OutputPath}", outputPath);
        var fileZipperCanEncryptHeaders = false;

        using var scope = _serviceScopeFactory.CreateScope();
        var fileZipperFactory = scope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        using (var fileZipper = fileZipperFactory.Create(outputPath, overwriteFileIfExists: true, password))
        {
            if (fileZipper.CanEncryptHeaders)
            {
                fileZipper.EncryptHeaders = plan.EncryptHeaders;
            }

            foreach (var entry in plan.Items)
            {
                AddEntryToZip(fileZipper, entry, string.Empty);
            }

            if (fileZipper.HasToBeSaved)
            {
                _logger.LogInformation("Saving zip to {OutputPath}", outputPath);
                fileZipper.Save();
            }

            fileZipperCanEncryptHeaders = fileZipper.CanEncryptHeaders;
        }

        if (!string.IsNullOrWhiteSpace(password) && plan.EncryptHeaders && !fileZipperCanEncryptHeaders)
        {
            _logger.LogInformation("Used file zipper doesn't encrypt headers by default. Fallback to encryption by double zipping");
            EncryptZipFile(outputPath, password);
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
        var filePath = fileEntry.FilePath;
        if (!string.IsNullOrEmpty(fileEntry.CronExpression))
        {
            var lastOccurence = CronExpressionResolver.GetLastOccurrenceBeforeDateTime(
                fileEntry.CronExpression,
                _dateTimeProvider.Now,
                fileEntry.GetTimeZoneInfo());
            filePath = SimplifiedPOSIXDateTimeResolver.Resolve(filePath, lastOccurence);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var sourceManager = scope.ServiceProvider.GetRequiredService<ISourceManager>();

        if (sourceManager.FileExists(filePath))
        {
            _logger.LogInformation("Adding file to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                                   filePath, string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/');
            zipper.AddFile(filePath, zipPath);
        }
        else if (sourceManager.DirectoryExists(filePath))
        {
            _logger.LogInformation("Adding directory to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                                   filePath, string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/');
            zipper.AddDirectory(filePath, zipPath);
        }
        else
        {
            _logger.LogWarning("File or directory not found: {FileEntryFilePath}", filePath);
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

    private void EncryptZipFile(string zipPath, string password)
    {
        var extension = Path.GetExtension(zipPath);
        var pathWithoutExtension = Path.GetFileNameWithoutExtension(zipPath);
        var innerZipName = pathWithoutExtension + ".inner" + extension;
        var directory = Path.GetDirectoryName(zipPath) ?? throw new InvalidOperationException("Zip file path does not have a valid directory.");
        var innerZipPath = Path.Combine(directory, innerZipName);

        File.Move(zipPath, innerZipPath, true);

        var fastZip = new FastZip()
        {
            Password = password,
            CreateEmptyDirectories = true,
            EntryEncryptionMethod = ZipEncryptionMethod.AES256
        };
        fastZip.CreateZip(zipPath, directory, false, innerZipName);

        File.Delete(innerZipPath);
    }
}