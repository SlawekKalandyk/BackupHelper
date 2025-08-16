using BackupHelper.Abstractions;
using BackupHelper.Sources.SMB;
using MediatR;

namespace BackupHelper.Api.Features.Credentials.SMB;

public record AddSMBCredentialCommand(ICredentialsProviderConfiguration CredentialsProviderConfiguration, SMBCredential Credential) : IRequest;


public class AddSMBCredentialCommandHandler : IRequestHandler<AddSMBCredentialCommand>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public AddSMBCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task Handle(AddSMBCredentialCommand request, CancellationToken cancellationToken)
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(request.CredentialsProviderConfiguration);
        credentialsProvider.SetCredential(request.Credential.ToCredentialEntry());

        return Task.CompletedTask;
    }
}