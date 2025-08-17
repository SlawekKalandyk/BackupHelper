using BackupHelper.Abstractions;

namespace BackupHelper.Api;

public class ApplicationDataHandler : IApplicationDataHandler
{
    public string GetApplicationDataPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var backupHelperPath = Path.Combine(appDataPath, "BackupHelper");
        Directory.CreateDirectory(backupHelperPath);

        return backupHelperPath;
    }

    public string GetBackupProfilesPath()
    {
        var backupProfilesPath = Path.Combine(GetApplicationDataPath(), "BackupProfiles");
        Directory.CreateDirectory(backupProfilesPath);

        return backupProfilesPath;
    }

    public string GetCredentialProfilesPath()
    {
        var credentialProfilesPath = Path.Combine(GetApplicationDataPath(), "CredentialProfiles");
        Directory.CreateDirectory(credentialProfilesPath);

        return credentialProfilesPath;
    }
}