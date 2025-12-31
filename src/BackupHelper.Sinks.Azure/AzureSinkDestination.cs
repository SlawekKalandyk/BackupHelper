using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.Azure;

public class AzureSinkDestination : ISinkDestination
{
    public const string SinkName = "Azure";
    public string Name => SinkName;

    public ISink CreateSink() => new AzureSink(this);
}