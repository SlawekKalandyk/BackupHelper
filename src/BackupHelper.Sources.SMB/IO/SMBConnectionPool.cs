using BackupHelper.Abstractions;
using BackupHelper.Abstractions.ResourcePooling;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.SMB;

internal class SMBConnectionPool : ResourcePoolBase<SMBConnection, SMBShareInfo>
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
        var credentialName = SMBCredentialHelper.GetSMBCredentialTitle(
            shareInfo.ServerIPAddress.ToString(),
            shareInfo.ShareName
        );
        var credential = _credentialsProvider.GetCredential(credentialName);

        if (credential == null)
            throw new InvalidOperationException(
                $"No credentials found for SMB share '{credentialName}'."
            );

        return new SMBCredential(
            shareInfo.ServerIPAddress.ToString(),
            shareInfo.ShareName,
            credential.Username,
            credential.Password!
        );
    }
}
