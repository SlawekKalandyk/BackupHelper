using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.BackupProfiles;

public record DeleteBackupProfileCommand(string ProfileName) : IRequest;

public class DeleteBackupProfileCommandHandler : IRequestHandler<DeleteBackupProfileCommand>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public DeleteBackupProfileCommandHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task Handle(DeleteBackupProfileCommand request, CancellationToken cancellationToken)
    {
        var backupProfilesPath = _applicationDataHandler.GetBackupProfilesPath();
        var profileFilePath = Path.Combine(backupProfilesPath, request.ProfileName);

        if (File.Exists(profileFilePath))
        {
            File.Delete(profileFilePath);
        }

        return Task.CompletedTask;
    }
}