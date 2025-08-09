namespace BackupHelper.Core.BackupZipping;

public interface IBackupPlanZipper
{
    void CreateZipFile(BackupPlan plan, string outputPath, string? password = null);
}