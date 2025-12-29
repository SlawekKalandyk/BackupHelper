namespace BackupHelper.Core.BackupZipping;

public interface IBackupPlanZipper
{
    void CreateZipFile(BackupPlan plan, string outputFileName, string? password = null);
}