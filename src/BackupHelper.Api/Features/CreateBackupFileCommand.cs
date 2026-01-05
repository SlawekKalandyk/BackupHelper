using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Features;

public record CreateBackupFileCommand(
    BackupPlan BackupPlan,
    string? OutputDirectory,
    string? Password = null
) : IRequest<CreateBackupFileCommandResult>;

public record CreateBackupFileCommandResult(string OutputFilePath);

public class CreateBackupFileCommandHandler
    : IRequestHandler<CreateBackupFileCommand, CreateBackupFileCommandResult>
{
    private readonly ILogger<CreateBackupFileCommandHandler> _logger;
    private readonly IBackupPlanZipper _backupPlanZipper;

    public CreateBackupFileCommandHandler(
        ILogger<CreateBackupFileCommandHandler> logger,
        IBackupPlanZipper backupPlanZipper
    )
    {
        _logger = logger;
        _backupPlanZipper = backupPlanZipper;
    }

    public Task<CreateBackupFileCommandResult> Handle(
        CreateBackupFileCommand request,
        CancellationToken cancellationToken
    )
    {
        var outputFilePath = BackupSavePathHelper.GetBackupSaveFilePath(
            request.OutputDirectory,
            request.BackupPlan.ZipFileNameSuffix
        );

        Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath)!);

        try
        {
            _backupPlanZipper.CreateZipFile(request.BackupPlan, outputFilePath, request.Password);
            _logger.LogInformation("Backup created at {BackupFilePath}", outputFilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup at {BackupFilePath}", outputFilePath);
            throw;
        }

        return Task.FromResult(new CreateBackupFileCommandResult(outputFilePath));
    }
}