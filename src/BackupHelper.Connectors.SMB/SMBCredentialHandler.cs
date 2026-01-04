using BackupHelper.Abstractions.Credentials;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Connectors.SMB;

public class SMBCredentialHandler : CredentialHandlerBase<SMBCredential>
{
    private readonly ILogger<SMBCredentialHandler> _logger;

    public SMBCredentialHandler(ILogger<SMBCredentialHandler> logger)
    {
        _logger = logger;
    }

    public override string Kind => SMBCredential.CredentialKind;

    protected override SMBCredential FromCredentialEntryCore(
        CredentialEntry entry,
        string localTitle
    )
    {
        var (server, shareName) = SMBCredentialHelper.DeconstructSMBCredentialTitle(localTitle);

        return new SMBCredential(
            Server: server,
            ShareName: shareName,
            Username: entry.Username,
            Password: entry.Password
        );
    }

    protected override Task<bool> TestConnectionAsyncCore(
        SMBCredential credential,
        CancellationToken cancellationToken
    )
    {
        var smbShareInfo = credential.GetSMBShareInfo();
        try
        {
            using var smbConnection = new SMBConnection(
                smbShareInfo.ServerIPAddress,
                string.Empty,
                smbShareInfo.ShareName,
                credential.Username,
                credential.Password
            );

            return Task.FromResult(smbConnection.TestConnection());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to connect to SMB share {SMBShare} with username {Username}. Reason: {ExMessage}",
                smbShareInfo,
                credential.Username,
                ex.Message
            );

            return Task.FromResult(false);
        }
    }
}
