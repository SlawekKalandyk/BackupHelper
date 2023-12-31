using BackupHelper.Core.FileZipping;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Features
{
    public record CreateBackupCommand(BackupConfiguration BackupConfiguration, string BackupFilePath) : IRequest<CreateBackupCommandResult>;
    public record CreateBackupCommandResult;

    public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, CreateBackupCommandResult>
    {
        private readonly ILogger _logger;

        public CreateBackupCommandHandler(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<CreateBackupCommandResult> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
        {
            var fileZipper = new BackupFileZipper(request.BackupConfiguration);
            fileZipper.CreateZipFile(request.BackupFilePath);

            return new CreateBackupCommandResult();
        }
    }
}
