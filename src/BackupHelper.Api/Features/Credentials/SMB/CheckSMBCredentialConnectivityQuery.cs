using BackupHelper.Abstractions;
using BackupHelper.Sources.SMB;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record CheckSMBCredentialConnectivityQuery(CredentialEntry CredentialEntry) : IRequest<bool>;

public class CheckSMBCredentialConnectivityQueryHandler
    : IRequestHandler<CheckSMBCredentialConnectivityQuery, bool>
{
    private readonly ILogger<CheckSMBCredentialConnectivityQueryHandler> _logger;

    public CheckSMBCredentialConnectivityQueryHandler(
        ILogger<CheckSMBCredentialConnectivityQueryHandler> logger
    )
    {
        _logger = logger;
    }

    public Task<bool> Handle(
        CheckSMBCredentialConnectivityQuery request,
        CancellationToken cancellationToken
    )
    {
        var smbShareInfo = SMBShareInfo.FromFilePath(request.CredentialEntry.Title);

        try
        {
            using var smbConnection = new SMBConnection(
                smbShareInfo.ServerIPAddress,
                string.Empty,
                smbShareInfo.ShareName,
                request.CredentialEntry.Username,
                request.CredentialEntry.Password
            );

            return Task.FromResult(smbConnection.TestConnection());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Failed to connect to SMB share {SMBShare} with username {Username}. Reason: {ExMessage}",
                smbShareInfo,
                request.CredentialEntry.Username,
                ex.Message
            );

            return Task.FromResult(false);
        }
    }
}
