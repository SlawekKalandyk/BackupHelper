using BackupHelper.Abstractions;

namespace BackupHelper.Tests.Shared;

public class TestCredentialsProviderFactory : ICredentialsProviderFactory
{
    public TestCredentialsProvider TestCredentialsProvider { get; } = new TestCredentialsProvider();

    public ICredentialsProvider Create(ICredentialsProviderConfiguration configuration)
        => TestCredentialsProvider;

    public void SetDefaultCredentialsProviderConfiguration(ICredentialsProviderConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    public ICredentialsProvider GetDefaultCredentialsProvider()
        => TestCredentialsProvider;

}

public record TestCredentialsProviderConfiguration : ICredentialsProviderConfiguration;

public class TestCredentialsProvider : ICredentialsProvider
{
    private readonly Dictionary<string, CredentialEntry> _credentials = new();

    public CredentialEntry? GetCredential(string credentialName)
        => _credentials.GetValueOrDefault(credentialName);

    public void SetCredential(CredentialEntry credentialEntry)
    {
        _credentials[credentialEntry.Title] = credentialEntry;
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        _credentials[credentialEntry.Title] = credentialEntry;
    }

    public void DeleteCredential(string credentialName)
    {
        _credentials.Remove(credentialName);
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials()
        => _credentials
            .Select(kvp => kvp.Value with { Title = kvp.Key })
            .ToList()
            .AsReadOnly();

    public void Dispose()
    {

    }
}