using BackupHelper.Abstractions.Credentials;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record UpdateCredentialCommand(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    CredentialEntry ExistingCredentialEntry,
    CredentialEntry NewCredentialEntry
) : IRequest;

public class UpdateCredentialCommandHandler : IRequestHandler<UpdateCredentialCommand>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public UpdateCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(UpdateCredentialCommand request, CancellationToken cancellationToken)
    {
        if (request.ExistingCredentialEntry.EntryTitle != request.NewCredentialEntry.EntryTitle)
        {
            throw new InvalidOperationException(
                $"Cannot update credential: provided titles are different. "
                    + $"Existing: '{request.ExistingCredentialEntry.EntryTitle}', New: '{request.NewCredentialEntry.EntryTitle}'."
            );
        }

        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        var credentials = credentialsProvider.GetCredentials();
        var credential = credentials.FirstOrDefault(c =>
            c.EntryTitle == request.NewCredentialEntry.EntryTitle
        );

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"Credential with title '{request.NewCredentialEntry.EntryTitle}' does not exist and cannot be updated."
            );
        }

        credentialsProvider.UpdateCredential(request.NewCredentialEntry);

        return Task.CompletedTask;
    }
}
