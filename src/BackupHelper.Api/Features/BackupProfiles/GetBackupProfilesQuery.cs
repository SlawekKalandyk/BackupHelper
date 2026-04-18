using BackupHelper.Abstractions;
using MediatR;
using Newtonsoft.Json;

namespace BackupHelper.Api.Features.BackupProfiles;

public record GetBackupProfilesQuery : IRequest<IReadOnlyCollection<BackupProfile>>;

public class GetBackupProfilesQueryHandler
    : IRequestHandler<GetBackupProfilesQuery, IReadOnlyCollection<BackupProfile>>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public GetBackupProfilesQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public async Task<IReadOnlyCollection<BackupProfile>> Handle(
        GetBackupProfilesQuery request,
        CancellationToken cancellationToken
    )
    {
        var backupProfilesPath = _applicationDataHandler.GetBackupProfilesPath();
        var backupProfiles = new List<BackupProfile>();

        foreach (var path in Directory.GetFiles(backupProfilesPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(path, cancellationToken);
            var backupProfile = JsonConvert.DeserializeObject<BackupProfile>(content);

            if (backupProfile != null)
            {
                backupProfiles.Add(backupProfile);
            }
        }

        return backupProfiles;
    }
}
