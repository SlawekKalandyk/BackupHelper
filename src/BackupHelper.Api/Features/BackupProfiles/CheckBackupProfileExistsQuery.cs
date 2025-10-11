using BackupHelper.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features.BackupProfiles;

public record CheckBackupProfileExistsQuery(string BackupProfileName) : IRequest<bool>;

public class CheckBackupProfileExistsQueryHandler
    : IRequestHandler<CheckBackupProfileExistsQuery, bool>
{
    private readonly IApplicationDataHandler _applicationDataHandler;

    public CheckBackupProfileExistsQueryHandler(IApplicationDataHandler applicationDataHandler)
    {
        _applicationDataHandler = applicationDataHandler;
    }

    public Task<bool> Handle(
        CheckBackupProfileExistsQuery request,
        CancellationToken cancellationToken
    )
    {
        var backupProfilePath = Path.Combine(
            _applicationDataHandler.GetBackupProfilesPath(),
            request.BackupProfileName
        );
        var backupProfileExists = File.Exists(backupProfilePath);

        return Task.FromResult(backupProfileExists);
    }
}
