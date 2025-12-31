namespace BackupHelper.Core.BackupZipping;

public interface IBackupPlanZipper
{
    void CreateZipFile(BackupPlan plan, string outputFilePath, string? password = null);
}