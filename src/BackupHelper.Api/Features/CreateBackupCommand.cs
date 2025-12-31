using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Utilities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Core.Features;

public record CreateBackupCommand(
    BackupPlan BackupPlan,
    string? OutputDirectory,
    string? Password = null
) : IRequest<CreateBackupCommandResult>;

public record CreateBackupCommandResult(string OutputFilePath);

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
        var outputFilePath = BackupSavePathHelper.GetBackupSaveFilePath(
            request.OutputDirectory,
            request.BackupPlan.ZipFileNameSuffix
        );

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

        return Task.FromResult(new CreateBackupCommandResult(outputFilePath));
    }
}