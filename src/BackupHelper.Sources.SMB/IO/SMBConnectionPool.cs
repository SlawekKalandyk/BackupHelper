using BackupHelper.Abstractions;
using BackupHelper.Abstractions.ConnectionPooling;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.SMB;

internal class SMBConnectionPool : ConnectionPoolBase<SMBConnection, SMBShareInfo>
{
    private readonly ICredentialsProvider _credentialsProvider;

    public SMBConnectionPool(ICredentialsProvider credentialsProvider, ILogger<SMBConnectionPool> logger)
        : base(logger, 5, TimeSpan.FromSeconds(90))
    {
        _credentialsProvider = credentialsProvider;
    }

    protected override SMBConnection CreateConnection(SMBShareInfo shareInfo)
    {
        var credential = GetCredential(shareInfo);

        return new SMBConnection(
            shareInfo.ServerIPAddress,
            string.Empty,
            shareInfo.ShareName,
            credential.Username,
            credential.Password);
    }

    protected override bool ValidateConnection(SMBConnection connection)
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

    protected override void DisposeConnection(SMBConnection connection)
    {
        connection.Dispose();
    }

    private SMBCredential GetCredential(SMBShareInfo shareInfo)
    {
        var credentialName = SMBCredentialHelper.GetSMBCredentialTitle(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName);
        var credential = _credentialsProvider.GetCredential(credentialName);

        if (credential == null)
            throw new InvalidOperationException($"No credentials found for SMB share '{credentialName}'.");

        return new SMBCredential(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName, credential.Username, credential.Password!);
    }
}