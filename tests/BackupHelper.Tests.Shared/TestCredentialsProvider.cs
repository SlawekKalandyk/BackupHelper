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

    public void Dispose()
    {
        TestCredentialsProvider.DisposeCredentials();
    }
}

public record TestCredentialsProviderConfiguration : ICredentialsProviderConfiguration
{
    public void Dispose()
    {
        // TODO release managed resources here
    }
}

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
        if (_credentials.TryGetValue(credentialEntry.EntryTitle, out var old))
            old.Dispose();
        _credentials[credentialEntry.EntryTitle] = credentialEntry;
    }

    public void UpdateCredential(CredentialEntry credentialEntry)
    {
        if (_credentials.TryGetValue(credentialEntry.EntryTitle, out var old))
            old.Dispose();
        _credentials[credentialEntry.EntryTitle] = credentialEntry;
    }

    public void DeleteCredential(CredentialEntry credentialEntry)
    {
        if (_credentials.Remove(credentialEntry.EntryTitle, out var old))
            old.Dispose();
    }

    public IReadOnlyCollection<CredentialEntry> GetCredentials() =>
        _credentials
            .Values.Select(v => new CredentialEntry(v.EntryTitle, v.Username, v.Password.Clone()))
            .ToList()
            .AsReadOnly();

    public void DisposeCredentials()
    {
        foreach (var entry in _credentials.Values)
            entry.Dispose();
        _credentials.Clear();
    }

    public void Dispose() { }
}