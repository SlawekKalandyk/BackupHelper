namespace BackupHelper.Abstractions;

public interface IApplicationDataHandler
{
    string GetApplicationDataPath();
    string GetBackupProfilesPath();
}