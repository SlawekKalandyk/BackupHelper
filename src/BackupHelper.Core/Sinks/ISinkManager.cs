using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Core.Sinks;

public interface ISinkManager
{
    ISink GetSink(ISinkDestination destination);
}