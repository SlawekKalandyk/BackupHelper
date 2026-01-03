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
    private readonly Dictionary<string, CredentialEntry> _credentials = new();

    public TestCredentialsProvider(CredentialHandlerRegistry credentialHandlerRegistry)
    {
        _credentialHandlerRegistry = credentialHandlerRegistry;
    }

    public CredentialEntry? GetCredential(string credentialName) =>
        _credentials.GetValueOrDefault(credentialName);

    public T? GetCredential<T>(string credentialLocalTitle)
        where T : ICredential
    {
        var title = CredentialHelper.ConstructCredentialTitle(
            _credentialHandlerRegistry.GetKindFor<T>(),
            credentialLocalTitle
        );

        return _credentialHandlerRegistry.TryGetCredentialFromEntry(
            _credentials[title],
            out T? credential
        )
            ? credential
            : default;
    }

    public void SetCredential(CredentialEntry credentialEntry)
    {
        _credentials[credentialEntry.Title] = credentialEntry;
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        _credentials[credentialEntry.Title] = credentialEntry;
    }

    public void DeleteCredential(CredentialEntry credentialEntry)
    {
        _credentials.Remove(credentialEntry.Title);
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials() =>
        _credentials.Select(kvp => kvp.Value with { Title = kvp.Key }).ToList().AsReadOnly();

    public void Dispose() { }
}