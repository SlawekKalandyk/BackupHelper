using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Core.Sinks;

public class SinkManager : ISinkManager
{
    private readonly IReadOnlyDictionary<string, ISinkFactory> _factoriesByKind;

    public SinkManager(IEnumerable<ISinkFactory> factories)
    {
        _factoriesByKind = factories.ToDictionary(r => r.Kind, StringComparer.OrdinalIgnoreCase);
    }

    public ISink GetSink(ISinkDestination destination)
    {
        if (!_factoriesByKind.TryGetValue(destination.Kind, out var factory))
        {
            throw new NotSupportedException(
                $"Sink for destination kind {destination.Kind} is not supported."
            );
        }

        return factory.CreateSink(destination);
    }
}