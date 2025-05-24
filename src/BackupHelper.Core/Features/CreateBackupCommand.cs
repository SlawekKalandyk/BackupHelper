using BackupHelper.Core.FileZipping;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Features
{
    public record CreateBackupCommand(BackupPlan BackupPlan, params string[] BackupFilePaths) : IRequest<CreateBackupCommandResult>;
    public record CreateBackupCommandResult;

    public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, CreateBackupCommandResult>
    {
        private readonly ILogger<CreateBackupCommandHandler> _logger;
        private readonly IBackupPlanZipper _backupPlanZipper;

        public CreateBackupCommandHandler(ILogger<CreateBackupCommandHandler> logger, IBackupPlanZipper backupPlanZipper)
        {
            _logger = logger;
            _backupPlanZipper = backupPlanZipper;
        }

        public Task<CreateBackupCommandResult> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
        {
            foreach (var backupFilePath in request.BackupFilePaths)
            {
                try
                {
                    _backupPlanZipper.CreateZipFile(request.BackupPlan, backupFilePath);
                    _logger.LogInformation("Backup created at {BackupFilePath}", backupFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create backup at {BackupFilePath}", backupFilePath);
                }
            }

            return Task.FromResult(new CreateBackupCommandResult());
        }
    }
}