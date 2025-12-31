namespace BackupHelper.Sinks.Abstractions;

public interface ISink
{
    ISinkDestination Destination { get; }
    string Description { get; }

    /// <summary>
    /// Uploads a file to the specified destination.
    /// </summary>
    Task UploadAsync(string sourceFilePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the sink is available and properly configured.
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}

public abstract class SinkBase<T> : ISink
    where T : ISinkDestination
{
    protected SinkBase(T destination)
    {
        TypedDestination = destination;
    }

    public ISinkDestination Destination => TypedDestination;
    public T TypedDestination { get; }
    public abstract string Description { get; }

    public abstract Task UploadAsync(
        string sourceFilePath,
        CancellationToken cancellationToken = default
    );

    public abstract Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
}