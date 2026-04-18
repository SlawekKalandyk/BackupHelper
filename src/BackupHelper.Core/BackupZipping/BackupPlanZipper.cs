using System.Text;
using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;
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

    public async Task CreateZipFileAsync(
        BackupPlan plan,
        string outputFilePath,
        SensitiveString? password = null,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogInformation("Creating backup file at {OutputPath}", outputFilePath);

        using var scope = _serviceScopeFactory.CreateScope();
        var fileZipperFactory = scope.ServiceProvider.GetRequiredService<IFileZipperFactory>();

        await using (
            var fileZipper = fileZipperFactory.Create(outputFilePath, overwriteFileIfExists: true)
        )
        {
            if (plan.ThreadLimit.HasValue)
                fileZipper.ThreadLimit = plan.ThreadLimit.Value;

            if (plan.MemoryLimitMB.HasValue)
                fileZipper.MemoryLimitMB = plan.MemoryLimitMB.Value;

            foreach (var entry in plan.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await AddEntryToZipAsync(
                    fileZipper,
                    entry,
                    string.Empty,
                    plan.CompressionLevel,
                    cancellationToken
                );
            }

            await fileZipper.WaitAsync(cancellationToken);

            if (fileZipper.HasToBeSaved)
            {
                _logger.LogInformation("Saving zip to {OutputPath}", outputFilePath);
                await fileZipper.SaveAsync(cancellationToken);
            }

            LogFailedFiles(fileZipper.FailedFiles);
        }

        if (password != null)
        {
            await EncryptZipFileAsync(outputFilePath, password, cancellationToken);
        }
    }

    private async Task AddEntryToZipAsync(
        IFileZipper zipper,
        BackupEntry entry,
        string zipPath,
        int? planCompressionLevel,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        switch (entry)
        {
            case BackupFileEntry fileEntry:
                await AddBackupFileEntryToZipAsync(
                    zipper,
                    fileEntry,
                    zipPath,
                    planCompressionLevel,
                    cancellationToken
                );
                break;
            case BackupDirectoryEntry dirEntry:
                await AddBackupDirectoryEntryToZipAsync(
                    zipper,
                    dirEntry,
                    zipPath,
                    planCompressionLevel,
                    cancellationToken
                );
                break;
        }
    }

    private async Task AddBackupFileEntryToZipAsync(
        IFileZipper zipper,
        BackupFileEntry fileEntry,
        string zipPath,
        int? planCompressionLevel,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

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

        if (await sourceManager.FileExistsAsync(filePath, cancellationToken))
        {
            _logger.LogInformation(
                "Adding file to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                filePath,
                string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/'
            );
            await zipper.AddFileAsync(filePath, zipPath, effectiveCompressionLevel, cancellationToken);
        }
        else if (await sourceManager.DirectoryExistsAsync(filePath, cancellationToken))
        {
            _logger.LogInformation(
                "Adding directory to zip file '{FileEntryFilePath}' under zip path '{ZipPath}'",
                filePath,
                string.IsNullOrEmpty(zipPath) ? "<root>" : zipPath + '/'
            );
            await zipper.AddDirectoryAsync(
                filePath,
                zipPath,
                effectiveCompressionLevel,
                cancellationToken
            );
        }
        else
        {
            _logger.LogWarning("File or directory not found: {FileEntryFilePath}", filePath);
        }
    }

    private async Task AddBackupDirectoryEntryToZipAsync(
        IFileZipper zipper,
        BackupDirectoryEntry dirEntry,
        string zipPath,
        int? planCompressionLevel,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var newZipPath = string.IsNullOrEmpty(zipPath)
            ? dirEntry.DirectoryName
            : Path.Combine(zipPath, dirEntry.DirectoryName);
        foreach (var subEntry in dirEntry.Items)
        {
            await AddEntryToZipAsync(
                zipper,
                subEntry,
                newZipPath,
                planCompressionLevel,
                cancellationToken
            );
        }
    }

    private Task EncryptZipFileAsync(
        string zipPath,
        SensitiveString password,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(() => EncryptZipFile(zipPath, password), cancellationToken);
    }

    private void EncryptZipFile(string zipPath, SensitiveString password)
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

        // FastZip requires a string password; the string cannot be zeroed due to .NET string
        // immutability. The SensitiveString bytes remain zeroable and are owned by the caller.
        var fastZip = new FastZip()
        {
            Password = password.Expose(),
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
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("The following files failed to be added to the zip archive:");
            foreach (var failedFile in failedFiles)
            {
                stringBuilder.AppendLine($" - {failedFile}");
            }
            _logger.LogWarning(stringBuilder.ToString());
        }
    }
}