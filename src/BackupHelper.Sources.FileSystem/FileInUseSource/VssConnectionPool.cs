using BackupHelper.Abstractions.ConnectionPooling;
using BackupHelper.Sources.FileSystem.Vss;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public class VssConnectionPool : ConnectionPoolBase<VssBackup, string>
{
    private readonly ILogger<VssBackup> _vssLogger;

    public VssConnectionPool(ILogger<VssConnectionPool> logger, ILogger<VssBackup> vssLogger)
        : base(logger, 5, TimeSpan.FromSeconds(90))
    {
        _vssLogger = vssLogger;
    }

    protected override VssBackup CreateConnection(string volume)
    {
        var vssBackup = new VssBackup(_vssLogger);
        vssBackup.Setup(volume);

        return vssBackup;
    }

    protected override void DisposeConnection(VssBackup vssBackup)
    {
        vssBackup.Dispose();
    }
}