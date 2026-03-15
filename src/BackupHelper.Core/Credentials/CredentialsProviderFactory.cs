using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Core.Credentials;

public class CredentialsProviderFactory : ICredentialsProviderFactory
{
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;
    private readonly Lock _configurationLock = new();
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

        lock (_configurationLock)
        {
            _defaultConfiguration?.Dispose();
            _defaultConfiguration = configuration;
        }
    }

    public void ClearDefaultCredentialsProviderConfiguration()
    {
        lock (_configurationLock)
        {
            _defaultConfiguration?.Dispose();
            _defaultConfiguration = null;
        }
    }

    public ICredentialsProvider GetDefaultCredentialsProvider()
    {
        lock (_configurationLock)
        {
            return _defaultConfiguration == null
                ? new NullCredentialsProvider()
                : Create(_defaultConfiguration);
        }
    }

    public void Dispose()
    {
        lock (_configurationLock)
        {
            _defaultConfiguration?.Dispose();
            _defaultConfiguration = null;
        }
    }
}