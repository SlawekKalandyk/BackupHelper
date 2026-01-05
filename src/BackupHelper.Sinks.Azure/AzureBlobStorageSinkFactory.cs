using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.Azure;
using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

public class AzureBlobStorageSinkFactory
    : SinkFactoryBase<AzureBlobStorageSink, AzureBlobStorageSinkDestination>
{
    private readonly ICredentialsProvider _credentialsProvider;

    public AzureBlobStorageSinkFactory(ICredentialsProvider credentialsProvider)
    {
        _credentialsProvider = credentialsProvider;
    }

    public override string Kind => AzureBlobStorageSinkDestination.SinkKind;

    public override AzureBlobStorageSink CreateSink(AzureBlobStorageSinkDestination destination)
    {
        var credential = _credentialsProvider.GetCredential<AzureBlobCredential>(
            new AzureBlobCredentialTitle(destination.AccountName)
        );

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"No {nameof(AzureBlobCredential)} found for account name {destination.AccountName}."
            );
        }

        return new AzureBlobStorageSink(destination, credential);
    }
}