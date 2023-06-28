using BackupHelper.Core.FileZipping;
using MediatR;

namespace BackupHelper.Core.Features
{
    public record CreateBackupCommand(BackupConfiguration BackupConfiguration, string BackupFilePath) : IRequest<CreateBackupCommandResult>;
    public record CreateBackupCommandResult;

    public class CreateBackupCommandHandler : IRequestHandler<CreateBackupCommand, CreateBackupCommandResult>
    {
        public async Task<CreateBackupCommandResult> Handle(CreateBackupCommand request, CancellationToken cancellationToken)
        {
            var fileZipper = new BackupFileZipper(request.BackupConfiguration);
            fileZipper.CreateZipFile(request.BackupFilePath);

            return new CreateBackupCommandResult();
        }
    }
}
