using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Core.Features;

public record AddSMBCredentialCommand(string IpAddress, string ShareName, string Username, string Password) : IRequest<AddSMBCredentialCommandResult>;

public record AddSMBCredentialCommandResult();

public class AddSMBCredentialCommandHandler : IRequestHandler<AddSMBCredentialCommand, AddSMBCredentialCommandResult>
{
    private readonly ICredentialsProvider _credentialsProvider;

    public AddSMBCredentialCommandHandler(ICredentialsProvider credentialsProvider)
    {
        _credentialsProvider = credentialsProvider;
    }

    public Task<AddSMBCredentialCommandResult> Handle(AddSMBCredentialCommand request, CancellationToken cancellationToken)
    {
        var title = $@"\\{request.IpAddress}\{request.ShareName}";
        _credentialsProvider.SetCredential(title, request.Username, request.Password);
        return Task.FromResult(new AddSMBCredentialCommandResult());
    }
}