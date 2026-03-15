using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Core.BackupZipping;

public interface IBackupPlanZipper
{
    void CreateZipFile(BackupPlan plan, string outputFilePath, SensitiveString? password = null);
}
