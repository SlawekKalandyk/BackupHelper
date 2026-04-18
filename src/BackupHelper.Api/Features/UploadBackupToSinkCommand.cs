using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Sinks;
using BackupHelper.Sinks.Abstractions;
using BackupHelper.Sinks.Azure;
using BackupHelper.Sinks.FileSystem;
using BackupHelper.Sinks.SMB;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Api.Features;

public record UploadBackupToSinkCommand(
    ISinkDestination SinkDestination,
    string SourceFilePath,
    int? MaxBackups = null
)
    : IRequest<BackupSinkUploadResult>;

public enum BackupSinkUploadStatus
{
    Uploaded,
    SkippedUnavailable,
    Failed,
}

public record BackupSinkUploadResult(
    string SinkDescription,
    BackupSinkUploadStatus Status,
    string? FailureMessage = null
);

public class UploadBackupToSinkCommandHandler
    : IRequestHandler<UploadBackupToSinkCommand, BackupSinkUploadResult>
{
    private readonly ISinkManager _sinkManager;
    private readonly ILogger<UploadBackupToSinkCommandHandler> _logger;

    public UploadBackupToSinkCommandHandler(
        ISinkManager sinkManager,
        ILogger<UploadBackupToSinkCommandHandler> logger
    )
    {
        _sinkManager = sinkManager;
        _logger = logger;
    }

    public async Task<BackupSinkUploadResult> Handle(
        UploadBackupToSinkCommand request,
        CancellationToken cancellationToken
    )
    {
        var sink = _sinkManager.GetSink(request.SinkDestination);

        try
        {
            var isAvailable = await sink.IsAvailableAsync(cancellationToken);

            if (!isAvailable)
            {
                return new BackupSinkUploadResult(
                    sink.Description,
                    BackupSinkUploadStatus.SkippedUnavailable
                );
            }

            try
            {
                await sink.UploadAsync(request.SourceFilePath, cancellationToken);

                var effectiveMaxBackups = ResolveMaxBackups(request.SinkDestination, request.MaxBackups);

                if (
                    effectiveMaxBackups.HasValue
                    && effectiveMaxBackups.Value > 0
                    && sink is IPrunableSink prunableSink
                )
                {
                    try
                    {
                        var backupFileName = Path.GetFileName(request.SourceFilePath);
                        await prunableSink.PruneBackupsAsync(
                            backupFileName,
                            effectiveMaxBackups.Value,
                            cancellationToken
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Backup upload succeeded for sink {SinkDescription}, but retention pruning failed.",
                            sink.Description
                        );
                    }
                }

                return new BackupSinkUploadResult(sink.Description, BackupSinkUploadStatus.Uploaded);
            }
            catch (Exception ex)
            {
                return new BackupSinkUploadResult(
                    sink.Description,
                    BackupSinkUploadStatus.Failed,
                    ex.GetBaseException().Message
                );
            }
        }
        finally
        {
            sink.Dispose();
        }
    }

    private static int? ResolveMaxBackups(ISinkDestination destination, int? backupPlanMaxBackups)
    {
        return destination switch
        {
            FileSystemSinkDestination sinkDestination => sinkDestination.MaxBackups ?? backupPlanMaxBackups,
            SMBSinkDestination sinkDestination => sinkDestination.MaxBackups ?? backupPlanMaxBackups,
            AzureBlobStorageSinkDestination sinkDestination => sinkDestination.MaxBackups ?? backupPlanMaxBackups,
            _ => backupPlanMaxBackups,
        };
    }
}