using BackupHelper.Abstractions;

public class NullCredentialsProvider : ICredentialsProvider
{
    public (string Username, string Password) GetCredential(string credentialName)
    {
        return (string.Empty, string.Empty);
    }

    public void SetCredential(string credentialName, string username, string password)
    {
        // No operation for null provider
    }

    public void Dispose()
    {
        // No resources to dispose
    }
}