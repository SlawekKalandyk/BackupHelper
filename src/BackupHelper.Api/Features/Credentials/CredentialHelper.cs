namespace BackupHelper.Api.Features.Credentials;

public static class CredentialHelper
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