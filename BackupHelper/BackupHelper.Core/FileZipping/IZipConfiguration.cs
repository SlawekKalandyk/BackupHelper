namespace BackupHelper.Core.FileZipping
{
    public interface IZipConfiguration
    {
        List<IZipConfigurationNode> Nodes { get; }
    }

    public interface IZipConfigurationNode
    {

    }
}
