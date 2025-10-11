using BackupHelper.Abstractions;
using BackupHelper.Sources.SMB;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record DeleteSMBCredentialCommand(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    string Server,
    string ShareName
) : IRequest;

public class DeleteSMBCredentialCommandHandler : IRequestHandler<DeleteSMBCredentialCommand>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public DeleteSMBCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(DeleteSMBCredentialCommand request, CancellationToken cancellationToken)
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(
            request.CredentialsProviderConfiguration
        );
        var title = SMBCredentialHelper.GetSMBCredentialTitle(request.Server, request.ShareName);
        credentialsProvider.DeleteCredential(title);

        return Task.CompletedTask;
    }
}
