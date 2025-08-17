using BackupHelper.Abstractions;
using MediatR;
using Newtonsoft.Json;

namespace BackupHelper.Api.Features.BackupProfiles;

public record GetBackupProfilesQuery : IRequest<IReadOnlyCollection<BackupProfile>>;

public class GetBackupProfilesQueryHandler : IRequestHandler<GetBackupProfilesQuery, IReadOnlyCollection<BackupProfile>>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public GetBackupProfilesQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task<IReadOnlyCollection<BackupProfile>> Handle(GetBackupProfilesQuery request, CancellationToken cancellationToken)
    {
        var backupProfilesPath = _applicationDataHandler.GetBackupProfilesPath();
        var backupProfiles = Directory.GetFiles(backupProfilesPath)
                                      .Select(path => JsonConvert.DeserializeObject<BackupProfile>(File.ReadAllText(path)));

        return Task.FromResult<IReadOnlyCollection<BackupProfile>>(backupProfiles.ToList());
    }
}