using BackupHelper.Core.BackupZipping;
using BackupHelper.Core.Sinks;
using BackupHelper.Sinks.Abstractions;
using MediatR;

namespace BackupHelper.Api.Features;

public record UploadBackupToSinkCommand(ISinkDestination SinkDestination, string SourceFilePath)
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

    public UploadBackupToSinkCommandHandler(ISinkManager sinkManager)
    {
        _sinkManager = sinkManager;
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
}