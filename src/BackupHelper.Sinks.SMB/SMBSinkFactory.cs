using BackupHelper.Abstractions.Credentials;
using BackupHelper.Connectors.SMB;
using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.SMB;

public class SMBSinkFactory : SinkFactoryBase<SMBSink, SMBSinkDestination>
{
    private readonly ICredentialsProvider _credentialsProvider;

    public SMBSinkFactory(ICredentialsProvider credentialsProvider)
    {
        _credentialsProvider = credentialsProvider;
    }

    public override string Kind => SMBSinkDestination.SinkKind;

    public override SMBSink CreateSink(SMBSinkDestination destination)
    {
        using var credential = _credentialsProvider.GetCredential<SMBCredential>(
            new SMBCredentialTitle(destination.Server, destination.ShareName)
        );

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"No {nameof(SMBCredential)} found for server {destination.Server} and share {destination.ShareName}."
            );
        }

        return new SMBSink(
            destination,
            credential.Server,
            credential.ShareName,
            credential.Username,
            credential.Password
        );
    }
}
