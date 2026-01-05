using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Core.Credentials;

public record NullCredentialsProviderConfiguration : ICredentialsProviderConfiguration;

public class NullCredentialsProvider : ICredentialsProvider
{
    public T? GetCredential<T>(CredentialEntryTitle credentialEntryTitle)
        where T : ICredential => default;

    public void SetCredential(CredentialEntry credentialEntry)
    {
        // No operation for null provider
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        // No operation for null provider
    }

    public void DeleteCredential(CredentialEntry credentialEntry)
    {
        // No operation for null provider
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials() => [];

    public void Dispose()
    {
        // No resources to dispose
    }
}