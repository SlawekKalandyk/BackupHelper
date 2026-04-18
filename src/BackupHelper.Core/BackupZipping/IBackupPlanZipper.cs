using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Core.BackupZipping;

public interface IBackupPlanZipper
{
    Task CreateZipFileAsync(
        BackupPlan plan,
        string outputFilePath,
        SensitiveString? password = null,
        CancellationToken cancellationToken = default
    );
}
