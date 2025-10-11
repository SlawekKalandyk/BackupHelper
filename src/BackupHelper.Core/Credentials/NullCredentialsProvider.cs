using BackupHelper.Abstractions;

namespace BackupHelper.Core.Credentials;

public record NullCredentialsProviderConfiguration : ICredentialsProviderConfiguration;

public class NullCredentialsProvider : ICredentialsProvider
{
    public CredentialEntry? GetCredential(string credentialName) => null;

    public void SetCredential(CredentialEntry credentialEntry)
    {
        // No operation for null provider
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        // No operation for null provider
    }

    public void DeleteCredential(string credentialName)
    {
        // No operation for null provider
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials() => [];

    public void Dispose()
    {
        // No resources to dispose
    }
}
