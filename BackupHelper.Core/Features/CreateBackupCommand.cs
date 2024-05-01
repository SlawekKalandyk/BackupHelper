using BackupHelper.Core.FileZipping;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Features
{
    public record CreateBackupCommand(BackupConfiguration BackupConfiguration, params string[] BackupFilePaths) : IRequest<CreateBackupCommandResult>;
    public record CreateBackupCommandResult;

    public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, CreateBackupCommandResult>
    {
        private readonly ILogger<CreateBackupCommandHandler> _logger;
        public CreateBackupCommandHandler(ILogger<CreateBackupCommandHandler> logger)
        {
            _logger = logger;
        }

        public Task<CreateBackupCommandResult> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
        {
            using var fileZipper = new BackupFileZipper(request.BackupConfiguration);
            foreach (var backupFilePath in request.BackupFilePaths)
            {
                try
                {
                    fileZipper.SaveZipFile(backupFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create backup at {backupFilePath}");
                }
            }

            return Task.FromResult(new CreateBackupCommandResult());
        }
    }
}
