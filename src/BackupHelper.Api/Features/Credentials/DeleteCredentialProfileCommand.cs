using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.Credentials;

public record DeleteCredentialProfileCommand(string CredentialProfileName) : IRequest;

public class DeleteCredentialProfileCommandHandler : IRequestHandler<DeleteCredentialProfileCommand>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public DeleteCredentialProfileCommandHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task Handle(DeleteCredentialProfileCommand request, CancellationToken cancellationToken)
    {
        var credentialProfilesPath = _applicationDataHandler.GetCredentialProfilesPath();
        var profileFilePath = Path.Combine(credentialProfilesPath, request.CredentialProfileName);

        if (File.Exists(profileFilePath))
        {
            File.Delete(profileFilePath);
        }

        return Task.CompletedTask;
    }
}
