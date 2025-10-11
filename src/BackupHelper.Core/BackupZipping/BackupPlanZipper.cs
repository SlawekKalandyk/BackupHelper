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

    public BackupPlanZipper(
        ILogger<BackupPlanZipper> logger,
        IServiceScopeFactory serviceScopeFactory,
        IDateTimeProvider dateTimeProvider
    )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _dateTimeProvider = dateTimeProvider;
    }

    public void CreateZipFile(BackupPlan plan, string outputPath, string? password = null)
    {
        _logger.LogInformation("Creating backup file at {OutputPath}", outputPath);

        using var scope = _serviceScopeFactory.CreateScope();
        var fileZipperFactory = scope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        using (var fileZipper = fileZipperFactory.Create(outputPath, overwriteFileIfExists: true))
        {
            if (plan.ThreadLimit.HasValue)
                fileZipper.ThreadLimit = plan.ThreadLimit.Value;

            if (plan.MemoryLimitMB.HasValue)
                fileZipper.MemoryLimitMB = plan.MemoryLimitMB.Value;

            foreach (var entry in plan.Items)
            {
                AddEntryToZip(fileZipper, entry, string.Empty, plan.CompressionLevel);
            }

            fileZipper.Wait();

            if (fileZipper.HasToBeSaved)
            {
                _logger.LogInformation("Saving zip to {OutputPath}", outputPath);
                fileZipper.Save();
            }

            LogFailedFiles(fileZipper.FailedFiles);
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            EncryptZipFile(outputPath, password);
        }
    }

    private void AddEntryToZip(
        IFileZipper zipper,
        BackupEntry entry,
        string zipPath,
        int? planCompressionLevel
    )
    {
        switch (entry)
        {
            case BackupFileEntry fileEntry:
                AddBackupFileEntryToZip(zipper, fileEntry, zipPath, planCompressionLevel);
                break;
            case BackupDirectoryEntry dirEntry:
                AddBackupDirectoryEntryToZip(zipper, dirEntry, zipPath, planCompressionLevel);
                break;
        }
    }

    private void AddBackupFileEntryToZip(
        IFileZipper zipper,
        BackupFileEntry fileEntry,
        string zipPath,
        int? planCompressionLevel
    )
    {
        var filePath = fileEntry.FilePath;
        if (!string.IsNullOrEmpty(fileEntry.CronExpression))
        {
            var lastOccurence = CronExpressionResolver.GetLastOccurrenceBeforeDateTime(
                fileEntry.CronExpression,
                _dateTimeProvider.Now,
                fileEntry.GetTimeZoneInfo()
            );
            filePath = SimplifiedPOSIXDateTimeResolver.Resolve(filePath, lastOccurence);
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var sourceManager = scope.ServiceProvider.GetRequiredService<ISourceManager>();

        var effectiveCompressionLevel = fileEntry.CompressionLevel ?? planCompressionLevel;

        if (sourceManager.FileExists(filePath))
        {
            _logger.LogInformation(
                "Adding file to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                filePath,
                string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/'
            );
            zipper.AddFile(filePath, zipPath, effectiveCompressionLevel);
        }
        else if (sourceManager.DirectoryExists(filePath))
        {
            _logger.LogInformation(
                "Adding directory to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                filePath,
                string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/'
            );
            zipper.AddDirectory(filePath, zipPath, effectiveCompressionLevel);
        }
        else
        {
            _logger.LogWarning("File or directory not found: {FileEntryFilePath}", filePath);
        }
    }

    private void AddBackupDirectoryEntryToZip(
        IFileZipper zipper,
        BackupDirectoryEntry dirEntry,
        string zipPath,
        int? planCompressionLevel
    )
    {
        var newZipPath = string.IsNullOrEmpty(zipPath)
            ? dirEntry.DirectoryName
            : Path.Combine(zipPath, dirEntry.DirectoryName);
        foreach (var subEntry in dirEntry.Items)
        {
            AddEntryToZip(zipper, subEntry, newZipPath, planCompressionLevel);
        }
    }

    private void EncryptZipFile(string zipPath, string password)
    {
        var extension = Path.GetExtension(zipPath);
        var pathWithoutExtension = Path.GetFileNameWithoutExtension(zipPath);
        var innerZipName = pathWithoutExtension + ".inner" + extension;
        var directory =
            Path.GetDirectoryName(zipPath)
            ?? throw new InvalidOperationException(
                "Zip file path does not have a valid directory."
            );
        var innerZipPath = Path.Combine(directory, innerZipName);

        File.Move(zipPath, innerZipPath, true);

        var fastZip = new FastZip()
        {
            Password = password,
            CreateEmptyDirectories = true,
            EntryEncryptionMethod = ZipEncryptionMethod.AES256,
            CompressionLevel = 0,
        };
        fastZip.CreateZip(zipPath, directory, false, innerZipName);

        File.Delete(innerZipPath);
    }

    private void LogFailedFiles(IReadOnlyCollection<string> failedFiles)
    {
        if (failedFiles.Count > 0)
        {
            var stringBuilder = new System.Text.StringBuilder();
            stringBuilder.AppendLine("The following files failed to be added to the zip archive:");
            foreach (var failedFile in failedFiles)
            {
                stringBuilder.AppendLine($" - {failedFile}");
            }
            _logger.LogWarning(stringBuilder.ToString());
        }
    }
}
