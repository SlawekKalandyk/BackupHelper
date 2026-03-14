using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Core.Credentials;

public class CredentialsProviderFactory : ICredentialsProviderFactory
{
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;
    private ICredentialsProviderConfiguration? _defaultConfiguration;

    public CredentialsProviderFactory(CredentialHandlerRegistry credentialHandlerRegistry)
    {
        _credentialHandlerRegistry = credentialHandlerRegistry;
    }

    public ICredentialsProvider Create(ICredentialsProviderConfiguration configuration)
    {
        return configuration switch
        {
            KeePassCredentialsProviderConfiguration keepass => new KeePassCredentialsProvider(
                keepass,
                _credentialHandlerRegistry
            ),
            NullCredentialsProviderConfiguration => new NullCredentialsProvider(),
            _ => throw new NotSupportedException(
                $"Configuration type '{configuration.GetType().Name}' is not supported."
            ),
        };
    }

    public void SetDefaultCredentialsProviderConfiguration(
        ICredentialsProviderConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _defaultConfiguration?.Dispose();
        _defaultConfiguration = configuration;
    }

    public ICredentialsProvider GetDefaultCredentialsProvider() =>
        Create(_defaultConfiguration ?? new NullCredentialsProviderConfiguration());

    public void Dispose()
    {
        _defaultConfiguration?.Dispose();
    }
}