namespace BackupHelper.Abstractions.Credentials;

public interface ICredentialsProviderFactory : IDisposable
{
    ICredentialsProvider Create(ICredentialsProviderConfiguration configuration);
    void SetDefaultCredentialsProviderConfiguration(
        ICredentialsProviderConfiguration configuration
    );
    void ClearDefaultCredentialsProviderConfiguration();
    ICredentialsProvider GetDefaultCredentialsProvider();
}