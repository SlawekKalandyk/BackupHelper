namespace BackupHelper.Sinks.Abstractions.Retention;

public sealed class BackupsPruningPlan
{
    public BackupsPruningPlan(
        BackupsManifest updatedManifest,
        IReadOnlyList<string> backupFileNamesToDelete
    )
    {
        UpdatedManifest = updatedManifest;
        BackupFileNamesToDelete = backupFileNamesToDelete;
    }

    public BackupsManifest UpdatedManifest { get; }
    public IReadOnlyList<string> BackupFileNamesToDelete { get; }
}
