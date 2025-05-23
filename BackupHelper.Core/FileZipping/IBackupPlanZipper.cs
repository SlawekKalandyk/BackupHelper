namespace BackupHelper.Core.FileZipping
{
    public interface IBackupPlanZipper
    {
        void CreateZipFile(BackupPlan plan, string outputPath);
    }
}