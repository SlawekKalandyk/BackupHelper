namespace BackupHelper.Sinks.Abstractions.Retention;

public static class BackupsRetentionPlanner
{
    private static readonly IReadOnlyList<string> DefaultProtectedFileNames =
    [
        BackupsRetentionConstants.ManifestFileName,
    ];

    public static BackupsPruningPlan CreatePlanForCaseInsensitiveStorage(
        BackupsManifest manifest,
        IEnumerable<string> existingBackupFileNames,
        string uploadedBackupFileName,
        int maxBackups,
        DateTime? utcNow = null
    )
    {
        return CreatePlanInternal(
            manifest,
            existingBackupFileNames,
            uploadedBackupFileName,
            maxBackups,
            StringComparer.OrdinalIgnoreCase,
            utcNow
        );
    }

    public static BackupsPruningPlan CreatePlanForCaseSensitiveStorage(
        BackupsManifest manifest,
        IEnumerable<string> existingBackupFileNames,
        string uploadedBackupFileName,
        int maxBackups,
        DateTime? utcNow = null
    )
    {
        return CreatePlanInternal(
            manifest,
            existingBackupFileNames,
            uploadedBackupFileName,
            maxBackups,
            StringComparer.Ordinal,
            utcNow
        );
    }

    private static BackupsPruningPlan CreatePlanInternal(
        BackupsManifest manifest,
        IEnumerable<string> existingBackupFileNames,
        string uploadedBackupFileName,
        int maxBackups,
        StringComparer fileNameComparer,
        DateTime? utcNow
    )
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest));
        }

        if (existingBackupFileNames == null)
        {
            throw new ArgumentNullException(nameof(existingBackupFileNames));
        }

        if (fileNameComparer == null)
        {
            throw new ArgumentNullException(nameof(fileNameComparer));
        }

        if (maxBackups < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBackups));
        }

        if (string.IsNullOrWhiteSpace(uploadedBackupFileName))
        {
            throw new ArgumentException(
                "Uploaded backup file name cannot be null or whitespace.",
                nameof(uploadedBackupFileName)
            );
        }

        var normalizedUtcNow = NormalizeUtc(utcNow ?? DateTime.UtcNow);
        var normalizedUploadedBackupFileName = uploadedBackupFileName.Trim();
        var normalizedProtectedFileNames = DefaultProtectedFileNames
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName.Trim())
            .ToHashSet(fileNameComparer);

        var normalizedExistingFileNames = existingBackupFileNames
            .Where(fileName => !string.IsNullOrWhiteSpace(fileName))
            .Select(fileName => fileName.Trim())
            .ToHashSet(fileNameComparer);
        var entriesByFileName = new Dictionary<string, BackupManifestEntry>(fileNameComparer);
        var manifestBackups = manifest.Backups ?? [];

        foreach (var entry in manifestBackups)
        {
            if (string.IsNullOrWhiteSpace(entry.FileName))
            {
                continue;
            }

            var fileName = entry.FileName.Trim();

            if (normalizedProtectedFileNames.Contains(fileName))
            {
                continue;
            }

            if (!normalizedExistingFileNames.Contains(fileName))
            {
                continue;
            }

            UpsertManifestEntry(
                entriesByFileName,
                new BackupManifestEntry
                {
                    FileName = fileName,
                    UploadedUtc = NormalizeUtc(entry.UploadedUtc),
                }
            );
        }

        if (!normalizedProtectedFileNames.Contains(normalizedUploadedBackupFileName))
        {
            UpsertManifestEntry(
                entriesByFileName,
                new BackupManifestEntry
                {
                    FileName = normalizedUploadedBackupFileName,
                    UploadedUtc = normalizedUtcNow,
                }
            );
        }

        var orderedEntries = entriesByFileName
            .Values.OrderByDescending(entry => entry.UploadedUtc)
            .ThenBy(entry => entry.FileName, fileNameComparer)
            .ToList();

        var backupFileNamesToDelete = orderedEntries
            .Skip(maxBackups)
            .Select(entry => entry.FileName)
            .Where(fileName => !normalizedProtectedFileNames.Contains(fileName))
            .ToList();

        var updatedManifest = new BackupsManifest
        {
            Version = manifest.Version > 0 ? manifest.Version : 1,
            UpdatedUtc = normalizedUtcNow,
            Backups = orderedEntries.Take(maxBackups).ToList(),
        };

        return new BackupsPruningPlan(updatedManifest, backupFileNamesToDelete);
    }

    private static void UpsertManifestEntry(
        IDictionary<string, BackupManifestEntry> entriesByFileName,
        BackupManifestEntry entry
    )
    {
        if (!entriesByFileName.TryGetValue(entry.FileName, out var existingEntry))
        {
            entriesByFileName[entry.FileName] = entry;
            return;
        }

        if (entry.UploadedUtc >= existingEntry.UploadedUtc)
        {
            entriesByFileName[entry.FileName] = entry;
        }
    }

    private static DateTime NormalizeUtc(DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Utc => dateTime,
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
        };
    }
}
