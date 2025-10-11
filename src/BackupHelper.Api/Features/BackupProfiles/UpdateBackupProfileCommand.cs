using BackupHelper.Abstractions;
using MediatR;
using Newtonsoft.Json;

namespace BackupHelper.Api.Features.BackupProfiles;

public record UpdateBackupProfileCommand(
    BackupProfile OriginalBackupProfile,
    BackupProfile UpdatedBackupProfile
) : IRequest;

public class UpdateBackupProfileCommandHandler : IRequestHandler<UpdateBackupProfileCommand>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public UpdateBackupProfileCommandHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task Handle(UpdateBackupProfileCommand request, CancellationToken cancellationToken)
    {
        var originalBackupProfilePath = Path.Combine(
            _applicationDataHandler.GetBackupProfilesPath(),
            request.OriginalBackupProfile.Name
        );
        var updatedBackupProfilePath = Path.Combine(
            _applicationDataHandler.GetBackupProfilesPath(),
            request.UpdatedBackupProfile.Name
        );

        if (request.OriginalBackupProfile.Name != request.UpdatedBackupProfile.Name)
        {
            if (File.Exists(updatedBackupProfilePath))
            {
                throw new InvalidOperationException(
                    $"A backup profile with the name '{request.UpdatedBackupProfile.Name}' already exists."
                );
            }

            File.WriteAllText(
                updatedBackupProfilePath,
                JsonConvert.SerializeObject(request.UpdatedBackupProfile, Formatting.Indented)
            );
            if (File.Exists(originalBackupProfilePath))
            {
                File.Delete(originalBackupProfilePath);
            }
        }
        else
        {
            File.WriteAllText(
                originalBackupProfilePath,
                JsonConvert.SerializeObject(request.UpdatedBackupProfile, Formatting.Indented)
            );
        }

        return Task.CompletedTask;
    }
}
