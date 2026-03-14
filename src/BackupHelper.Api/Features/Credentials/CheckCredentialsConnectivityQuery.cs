using BackupHelper.Abstractions.Credentials;
using BackupHelper.Api.Features.Credentials.CredentialProfiles;
using BackupHelper.Core.Credentials;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record CheckCredentialsConnectivityQuery(
    string CredentialProfileName,
    SensitiveString CredentialProfilePassword
) : IRequest<IReadOnlyCollection<IDisplayableCredentialEntry>>;

public class CheckCredentialsConnectivityQueryHandler
    : IRequestHandler<
        CheckCredentialsConnectivityQuery,
        IReadOnlyCollection<IDisplayableCredentialEntry>
    >
{
    private readonly IMediator _mediator;
    private readonly CredentialHandlerRegistry _credentialHandlerRegistry;

    public CheckCredentialsConnectivityQueryHandler(
        IMediator mediator,
        CredentialHandlerRegistry credentialHandlerRegistry
    )
    {
        _mediator = mediator;
        _credentialHandlerRegistry = credentialHandlerRegistry;
    }

    public async Task<IReadOnlyCollection<IDisplayableCredentialEntry>> Handle(
        CheckCredentialsConnectivityQuery request,
        CancellationToken cancellationToken
    )
    {
        var credentialProfile = await _mediator.Send(
            new GetCredentialProfileQuery(
                request.CredentialProfileName,
                // Create a fresh SensitiveString so GetCredentialProfileQuery owns its copy.
                request.CredentialProfilePassword.Clone()
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
            var isConnected = await _credentialHandlerRegistry.TestConnectionAsync(
                credentialEntry,
                cancellationToken
            );

            if (!isConnected)
                nonConnectedEntries.Add(credentialEntry);
        }

        return nonConnectedEntries;
    }
}