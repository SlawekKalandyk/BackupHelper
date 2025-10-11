namespace BackupHelper.Sources.SMB;

public static class SMBCredentialHelper
{
    /// <summary>
    /// Output format: \\server\shareName
    /// </summary>
    public static string GetSMBCredentialTitle(string server, string shareName)
    {
        if (string.IsNullOrWhiteSpace(server))
            throw new ArgumentException("Server name cannot be null or empty.", nameof(server));

        if (string.IsNullOrWhiteSpace(shareName))
            throw new ArgumentException("Share name cannot be null or empty.", nameof(shareName));

        return $@"\\{server}\{shareName}";
    }

    /// <summary>
    /// Expected format: \\server\shareName
    /// </summary>
    public static (string server, string shareName) DeconstructSMBCredentialTitle(
        string credentialTitle
    )
    {
        if (string.IsNullOrWhiteSpace(credentialTitle))
            throw new ArgumentException(
                "Credential title cannot be null or empty.",
                nameof(credentialTitle)
            );

        if (!credentialTitle.StartsWith(@"\\"))
            throw new ArgumentException(
                "Credential title must start with \\\\",
                nameof(credentialTitle)
            );

        var parts = credentialTitle.Substring(2).Split('\\', 2);

        if (
            parts.Length != 2
            || string.IsNullOrWhiteSpace(parts[0])
            || string.IsNullOrWhiteSpace(parts[1])
        )
            throw new ArgumentException(
                "Credential title must be in the format \\\\server\\shareName.",
                nameof(credentialTitle)
            );

        return (parts[0], parts[1]);
    }
}
