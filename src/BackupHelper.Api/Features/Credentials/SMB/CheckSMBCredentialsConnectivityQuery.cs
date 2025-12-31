using BackupHelper.Abstractions;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record CheckSMBCredentialsConnectivityQuery(
    string CredentialProfileName,
    string CredentialProfilePassword
) : IRequest<IReadOnlyCollection<IDisplayableCredentialEntry>>;

public class CheckSMBCredentialsConnectivityQueryHandler
    : IRequestHandler<
        CheckSMBCredentialsConnectivityQuery,
        IReadOnlyCollection<IDisplayableCredentialEntry>
    >
{
    private readonly IMediator _mediator;

    public CheckSMBCredentialsConnectivityQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IReadOnlyCollection<IDisplayableCredentialEntry>> Handle(
        CheckSMBCredentialsConnectivityQuery request,
        CancellationToken cancellationToken
    )
    {
        var credentialProfile = await _mediator.Send(
            new GetCredentialProfileQuery(
                request.CredentialProfileName,
                request.CredentialProfilePassword
            ),
            cancellationToken
        );

        if (credentialProfile == null)
        {
            throw new ArgumentException(
                $"Credential profile '{request.CredentialProfileName}' not found."
            );
        }

        var nonConnectedEntries = new List<IDisplayableCredentialEntry>();

        foreach (var credentialEntry in credentialProfile.Credentials)
        {
            if (IsSMBCredential(credentialEntry))
            {
                var isConnected = await _mediator.Send(
                    new CheckSMBCredentialConnectivityQuery(credentialEntry),
                    cancellationToken
                );
                if (!isConnected)
                    nonConnectedEntries.Add(credentialEntry);
            }
        }

        return nonConnectedEntries;
    }

    private bool IsSMBCredential(CredentialEntry credentialEntry) => true;
}