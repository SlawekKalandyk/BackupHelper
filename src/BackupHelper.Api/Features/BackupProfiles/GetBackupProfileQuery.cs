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

    public Task<BackupProfile?> Handle(GetBackupProfileQuery request, CancellationToken cancellationToken)
    {
        var backupProfilesPath = _applicationDataHandler.GetBackupProfilesPath();
        var backupProfileFilePath = Path.Combine(backupProfilesPath, request.Name);

        if (!File.Exists(backupProfileFilePath))
        {
            return Task.FromResult<BackupProfile?>(null);
        }

        var backupProfileJson = File.ReadAllText(backupProfileFilePath);
        var backupProfile = JsonConvert.DeserializeObject<BackupProfile>(backupProfileJson);

        return Task.FromResult(backupProfile);
    }
}