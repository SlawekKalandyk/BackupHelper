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
        private readonly IFileZipperFactory _fileZipperFactory;

        public CreateBackupCommandHandler(ILogger<CreateBackupCommandHandler> logger, IFileZipperFactory fileZipperFactory)
        {
            _logger = logger;
            _fileZipperFactory = fileZipperFactory;
        }

        public Task<CreateBackupCommandResult> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
        {
            foreach (var backupFilePath in request.BackupFilePaths)
            {
                try
                {
                    using var fileZipper = _fileZipperFactory.Create();
                    request.BackupPlan.CreateZipFile(backupFilePath, fileZipper);
                    _logger.LogInformation($"Backup created at {backupFilePath}");
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