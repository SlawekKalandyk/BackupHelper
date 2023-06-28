using BackupHelper.Core.FileZipping;

namespace BackupHelper.Core.DataTransfer
{
    public record BackupDto(BackupConfiguration BackupConfiguration, string BackupFilePath) { }
}
