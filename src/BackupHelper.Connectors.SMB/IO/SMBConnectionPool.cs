using BackupHelper.Abstractions.Credentials;
using BackupHelper.Abstractions.ResourcePooling;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Connectors.SMB.IO;

public class SMBConnectionPool : ResourcePoolBase<SMBConnection, SMBShareInfo>
{
    private readonly ICredentialsProvider _credentialsProvider;

    public SMBConnectionPool(
        ICredentialsProvider credentialsProvider,
        ILogger<SMBConnectionPool> logger
    )
        : base(logger, 5, TimeSpan.FromSeconds(90))
    {
        _credentialsProvider = credentialsProvider;
    }

    protected override SMBConnection CreateResource(SMBShareInfo shareInfo)
    {
        var credential = GetCredential(shareInfo);

        return new SMBConnection(
            shareInfo.ServerIPAddress,
            string.Empty,
            shareInfo.ShareName,
            credential.Username,
            credential.Password
        );
    }

    protected override bool ValidateResource(SMBConnection connection)
    {
        try
        {
            return connection.IsConnected && connection.TestConnection();
        }
        catch
        {
            return false;
        }
    }

    protected override void DisposeResource(SMBConnection connection)
    {
        connection.Dispose();
    }

    private SMBCredential GetCredential(SMBShareInfo shareInfo)
    {
        var credentialTitle = new SMBCredentialTitle(shareInfo);
        var credential = _credentialsProvider.GetCredential<SMBCredential>(credentialTitle);

        if (credential == null)
            throw new InvalidOperationException(
                $"No credentials found for SMB share '{credentialTitle}'."
            );

        return new SMBCredential(
            shareInfo.ServerIPAddress.ToString(),
            shareInfo.ShareName,
            credential.Username,
            credential.Password!
        );
    }
}