using BackupHelper.Core.BackupZipping;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Features;

public record CreateBackupCommand(
    BackupPlan BackupPlan,
    string BackupFilePath,
    string? Password = null
) : IRequest<CreateBackupCommandResult>;

public record CreateBackupCommandResult;

public class CreateBackupCommandHandler
    : IRequestHandler<CreateBackupCommand, CreateBackupCommandResult>
{
    private readonly ILogger<CreateBackupCommandHandler> _logger;
    private readonly IBackupPlanZipper _backupPlanZipper;

    public CreateBackupCommandHandler(
        ILogger<CreateBackupCommandHandler> logger,
        IBackupPlanZipper backupPlanZipper
    )
    {
        _logger = logger;
        _backupPlanZipper = backupPlanZipper;
    }

    public Task<CreateBackupCommandResult> Handle(
        CreateBackupCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            _backupPlanZipper.CreateZipFile(
                request.BackupPlan,
                request.BackupFilePath,
                request.Password
            );
            _logger.LogInformation("Backup created at {BackupFilePath}", request.BackupFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to create backup at {BackupFilePath}",
                request.BackupFilePath
            );
        }

        return Task.FromResult(new CreateBackupCommandResult());
    }
}
