using BackupHelper.Abstractions;
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
        _defaultConfiguration =
            configuration
            ?? throw new ArgumentNullException(
                nameof(configuration),
                "Configuration cannot be null."
            );
    }

    public ICredentialsProvider GetDefaultCredentialsProvider() =>
        Create(_defaultConfiguration ?? new NullCredentialsProviderConfiguration());
}
