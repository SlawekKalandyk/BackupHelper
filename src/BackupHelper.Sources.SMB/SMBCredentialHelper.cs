namespace BackupHelper.Sources.SMB;

public static class SMBCredentialHelper
{
    public static string GetSMBCredentialTitle(string server, string shareName)
    {
        if (string.IsNullOrWhiteSpace(server))
        {
            throw new ArgumentException("Server name cannot be null or empty.", nameof(server));
        }

        if (string.IsNullOrWhiteSpace(shareName))
        {
            throw new ArgumentException("Share name cannot be null or empty.", nameof(shareName));
        }

        return $@"\\{server}\{shareName}";
    }
}