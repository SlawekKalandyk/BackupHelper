using BackupHelper.Core.FileZipping;

namespace BackupHelper.Core.DataTransfer
{
    public record SaveBackupConfigDto(BackupConfiguration BackupConfiguration, string ConfigurationSavePath);
}
