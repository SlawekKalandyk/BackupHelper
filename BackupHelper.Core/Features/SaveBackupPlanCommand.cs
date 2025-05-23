using BackupHelper.Core.FileZipping;
using MediatR;

namespace BackupHelper.Core.Features
{
    public record SaveBackupPlanCommand(BackupPlan BackupPlan, string ConfigurationSavePath) : IRequest<SaveBackupPlanCommandResult>;

    public record SaveBackupPlanCommandResult;

    public class SaveBackupPlanCommandHandler : IRequestHandler<SaveBackupPlanCommand, SaveBackupPlanCommandResult>
    {
        public async Task<SaveBackupPlanCommandResult> Handle(SaveBackupPlanCommand request, CancellationToken cancellationToken)
        {
            request.BackupPlan.ToJsonFile(request.ConfigurationSavePath);

            return new SaveBackupPlanCommandResult();
        }
    }
}