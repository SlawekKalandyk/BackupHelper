using BackupHelper.Abstractions;
using MediatR;
using Newtonsoft.Json;

namespace BackupHelper.Api.Features.BackupProfiles;

public record GetBackupProfileQuery(string Name) : IRequest<BackupProfile?>;

public class GetBackupProfileQueryHandler : IRequestHandler<GetBackupProfileQuery, BackupProfile?>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public GetBackupProfileQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<BackupProfile?> Handle(
        GetBackupProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        var backupProfilesPath = _applicationDataHandler.GetBackupProfilesPath();
        var backupProfileFilePath = Path.Combine(backupProfilesPath, request.Name);

        if (!File.Exists(backupProfileFilePath))
        {
            return null;
        }

        var backupProfileJson = await File.ReadAllTextAsync(
            backupProfileFilePath,
            cancellationToken
        );
        var backupProfile = JsonConvert.DeserializeObject<BackupProfile>(backupProfileJson);

        return backupProfile;
    }
}
