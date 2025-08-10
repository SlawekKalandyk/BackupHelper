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
    private readonly Dictionary<string, (string Username, string Password)> _credentials = new();

    public (string Username, string Password) GetCredential(string credentialName)
        => _credentials[credentialName];

    public void SetCredential(string credentialName, string username, string password)
    {
        _credentials[credentialName] = (username, password);
    }

    public void Dispose()
    {

    }
}