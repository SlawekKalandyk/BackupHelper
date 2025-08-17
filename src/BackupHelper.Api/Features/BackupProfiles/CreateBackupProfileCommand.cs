using BackupHelper.Abstractions;
using MediatR;
using Newtonsoft.Json;

namespace BackupHelper.Api.Features.BackupProfiles;

public record CreateBackupProfileCommand(string Name, string BackupPlanLocation, string BackupDirectory, string KeePassDbLocation)
    : IRequest;


public class CreateBackupProfileCommandHandler : IRequestHandler<CreateBackupProfileCommand>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public CreateBackupProfileCommandHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task Handle(CreateBackupProfileCommand request, CancellationToken cancellationToken)
    {
        var backupProfilePath = Path.Combine(_applicationDataHandler.GetBackupProfilesPath(), request.Name);

        if (File.Exists(backupProfilePath))
        {
            throw new InvalidOperationException($"Backup profile with name '{request.Name}' already exists.");
        }

        var backupProfile = new BackupProfile(
            request.Name,
            request.BackupPlanLocation,
            request.BackupDirectory,
            request.KeePassDbLocation);

        File.WriteAllText(backupProfilePath, JsonConvert.SerializeObject(backupProfile, Formatting.Indented));

        return Task.CompletedTask;
    }
}