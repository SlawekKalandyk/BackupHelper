namespace BackupHelper.Abstractions.Credentials;

public interface ICredentialsProviderFactory
{
    ICredentialsProvider Create(ICredentialsProviderConfiguration configuration);
    void SetDefaultCredentialsProviderConfiguration(
        ICredentialsProviderConfiguration configuration
    );
    ICredentialsProvider GetDefaultCredentialsProvider();
}
