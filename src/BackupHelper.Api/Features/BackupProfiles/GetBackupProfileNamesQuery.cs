using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.BackupProfiles;

public record GetBackupProfileNamesQuery : IRequest<IReadOnlyList<string>>;

public class GetBackupProfileNamesQueryHandler : IRequestHandler<GetBackupProfileNamesQuery, IReadOnlyList<string>>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public GetBackupProfileNamesQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task<IReadOnlyList<string>> Handle(GetBackupProfileNamesQuery request, CancellationToken cancellationToken)
    {
        var backupProfilesPath = _applicationDataHandler.GetBackupProfilesPath();
        var backupProfiles = Directory.GetFiles(backupProfilesPath).Select(Path.GetFileName);

        return Task.FromResult<IReadOnlyList<string>>(backupProfiles.ToList());
    }
}