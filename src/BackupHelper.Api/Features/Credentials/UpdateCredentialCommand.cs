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
        if (request.ExistingCredentialEntry.Title != request.NewCredentialEntry.Title)
        {
            throw new InvalidOperationException(
                $"Cannot update credential: provided titles are different. "
                    + $"Existing: '{request.ExistingCredentialEntry.Title}', New: '{request.NewCredentialEntry.Title}'."
            );
        }

        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        var credentials = credentialsProvider.GetCredentials();
        var credential = credentials.FirstOrDefault(c =>
            c.Title == request.NewCredentialEntry.Title
        );

        if (credential == null)
        {
            throw new InvalidOperationException(
                $"Credential with title '{request.NewCredentialEntry.Title}' does not exist and cannot be updated."
            );
        }

        credentialsProvider.UpdateCredential(request.NewCredentialEntry);

        return Task.CompletedTask;
    }
}