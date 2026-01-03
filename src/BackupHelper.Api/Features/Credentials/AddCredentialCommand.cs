using BackupHelper.Abstractions.Credentials;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record AddCredentialCommand(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    CredentialEntry CredentialEntry
) : IRequest;

public class AddCredentialCommandHandler : IRequestHandler<AddCredentialCommand>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public AddCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(AddCredentialCommand request, CancellationToken cancellationToken)
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        credentialsProvider.SetCredential(request.CredentialEntry);

        return Task.CompletedTask;
    }
}