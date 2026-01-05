using BackupHelper.Abstractions.Credentials;
using BackupHelper.Core.Credentials;

namespace BackupHelper.Tests.Shared;

public class TestCredentialsProviderFactory : ICredentialsProviderFactory
{
    public TestCredentialsProvider TestCredentialsProvider { get; }

    public TestCredentialsProviderFactory(CredentialHandlerRegistry credentialHandlerRegistry)
    {
        TestCredentialsProvider = new TestCredentialsProvider(credentialHandlerRegistry);
    }

    public ICredentialsProvider Create(ICredentialsProviderConfiguration configuration) =>
        TestCredentialsProvider;

    public void SetDefaultCredentialsProviderConfiguration(
        ICredentialsProviderConfiguration configuration
    )
    {
        throw new NotImplementedException();
    }

    public ICredentialsProvider GetDefaultCredentialsProvider() => TestCredentialsProvider;
}

public record TestCredentialsProviderConfiguration : ICredentialsProviderConfiguration;

public class TestCredentialsProvider : ICredentialsProvider
{
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;
    private readonly Dictionary<CredentialEntryTitle, CredentialEntry> _credentials = new();

    public TestCredentialsProvider(CredentialHandlerRegistry credentialHandlerRegistry)
    {
        _credentialHandlerRegistry = credentialHandlerRegistry;
    }

    public T? GetCredential<T>(CredentialEntryTitle credentialEntryTitle)
        where T : ICredential
    {
        return _credentialHandlerRegistry.TryGetCredentialFromEntry(
            _credentials[credentialEntryTitle],
            out T? credential
        )
            ? credential
            : default;
    }

    public void SetCredential(CredentialEntry credentialEntry)
    {
        _credentials[credentialEntry.EntryTitle] = credentialEntry;
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        _credentials[credentialEntry.EntryTitle] = credentialEntry;
    }

    public void DeleteCredential(CredentialEntry credentialEntry)
    {
        _credentials.Remove(credentialEntry.EntryTitle);
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials() =>
        _credentials.Select(kvp => kvp.Value with { EntryTitle = kvp.Key }).ToList().AsReadOnly();

    public void Dispose() { }
}