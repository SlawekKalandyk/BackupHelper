namespace BackupHelper.Sinks.Abstractions;

public interface ISinkFactory
{
    string Kind { get; }
    ISink CreateSink(ISinkDestination destination);
}

public interface ISinkFactory<TSink, TSinkDestination> : ISinkFactory
    where TSink : ISink
    where TSinkDestination : ISinkDestination
{
    TSink CreateSink(TSinkDestination destination);
}

public abstract class SinkFactoryBase<TSink, TSinkDestination>
    : ISinkFactory<TSink, TSinkDestination>
    where TSink : ISink
    where TSinkDestination : ISinkDestination
{
    public abstract string Kind { get; }

    public abstract TSink CreateSink(TSinkDestination destination);

    ISink ISinkFactory.CreateSink(ISinkDestination destination)
    {
        if (destination is not TSinkDestination typedDestination)
        {
            throw new ArgumentException(
                $"Invalid sink destination type. Expected {typeof(TSinkDestination).FullName}, but got {destination.GetType().FullName}."
            );
        }

        return CreateSink(typedDestination);
    }
}