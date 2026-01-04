using BackupHelper.Sinks.Abstractions;

namespace BackupHelper.Sinks.FileSystem;

public class FileSystemSinkFactory : SinkFactoryBase<FileSystemSink, FileSystemSinkDestination>
{
    public override string Kind => FileSystemSinkDestination.SinkKind;

    public override FileSystemSink CreateSink(FileSystemSinkDestination destination)
    {
        return new FileSystemSink(destination);
    }
}
