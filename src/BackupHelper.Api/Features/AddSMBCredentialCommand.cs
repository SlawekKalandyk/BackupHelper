using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Core.Features;

public record AddSMBCredentialCommand(
    ICredentialsProviderConfiguration CredentialsProviderConfiguration,
    string IpAddress,
    string ShareName,
    string Username,
    string Password)
    : IRequest<AddSMBCredentialCommandResult>;

public record AddSMBCredentialCommandResult();

public class AddSMBCredentialCommandHandler : IRequestHandler<AddSMBCredentialCommand, AddSMBCredentialCommandResult>
{
    private readonly ICredentialsProviderFactory _credentialsProviderFactory;

    public AddSMBCredentialCommandHandler(ICredentialsProviderFactory credentialsProviderFactory)
    {
        _credentialsProviderFactory = credentialsProviderFactory;
    }

    public Task<AddSMBCredentialCommandResult> Handle(AddSMBCredentialCommand request, CancellationToken cancellationToken)
    {
        using var credentialsProvider = _credentialsProviderFactory.Create(request.CredentialsProviderConfiguration);
        var title = $@"\\{request.IpAddress}\{request.ShareName}";
        credentialsProvider.SetCredential(title, request.Username, request.Password);

        return Task.FromResult(new AddSMBCredentialCommandResult());
    }
}