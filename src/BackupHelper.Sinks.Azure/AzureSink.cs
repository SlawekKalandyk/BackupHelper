using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

internal class AzureSink : SinkBase<AzureSinkDestination>
{
    public AzureSink(AzureSinkDestination destination)
        : base(destination) { }

    public override string Description { get; }

    public override Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    ) => throw new NotImplementedException();

    public override Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}