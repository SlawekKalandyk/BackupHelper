namespace BackupHelper.Abstractions;

public interface ICredentialsProviderFactory
{
    ICredentialsProvider Create(ICredentialsProviderConfiguration configuration);
    void SetDefaultCredentialsProviderConfiguration(
        ICredentialsProviderConfiguration configuration
    );
    ICredentialsProvider GetDefaultCredentialsProvider();
}
