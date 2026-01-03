using BackupHelper.Abstractions.Credentials;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record DeleteCredentialCommand(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    CredentialEntry CredentialEntry
) : IRequest;

public class DeleteCredentialCommandHandler : IRequestHandler<DeleteCredentialCommand>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public DeleteCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(DeleteCredentialCommand request, CancellationToken cancellationToken)
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        credentialsProvider.DeleteCredential(request.CredentialEntry);

        return Task.CompletedTask;
    }
}