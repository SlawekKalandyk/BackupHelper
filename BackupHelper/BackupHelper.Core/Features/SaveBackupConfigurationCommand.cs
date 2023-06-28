using BackupHelper.Core.FileZipping;
using MediatR;

namespace BackupHelper.Core.Features
{
    public record SaveBackupConfigurationCommand(BackupConfiguration BackupConfiguration, string ConfigurationSavePath) : IRequest<SaveBackupConfigurationCommandResult>;
    public record SaveBackupConfigurationCommandResult;

    public class SaveBackupConfigurationCommandHandler : IRequestHandler<SaveBackupConfigurationCommand, SaveBackupConfigurationCommandResult>
    {
        public async Task<SaveBackupConfigurationCommandResult> Handle(SaveBackupConfigurationCommand request, CancellationToken cancellationToken)
        {
            request.BackupConfiguration.ToJsonFile(request.ConfigurationSavePath);

            return new SaveBackupConfigurationCommandResult();
        }
    }
}
