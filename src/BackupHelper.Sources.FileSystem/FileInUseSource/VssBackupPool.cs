using BackupHelper.Abstractions.ResourcePooling;
using BackupHelper.Sources.FileSystem.Vss;
using Microsoft.Extensions.Logging;

namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public class VssBackupPool : ResourcePoolBase<VssBackup, string>
{
    private readonly ILogger<VssBackup> _vssLogger;

    public VssBackupPool(ILogger<VssBackupPool> logger, ILogger<VssBackup> vssLogger)
        : base(logger, 5, TimeSpan.FromSeconds(90))
    {
        _vssLogger = vssLogger;
    }

    protected override VssBackup CreateResource(string volume)
    {
        var vssBackup = new VssBackup(_vssLogger);
        vssBackup.Setup(volume);

        return vssBackup;
    }

    protected override void DisposeResource(VssBackup vssBackup)
    {
        vssBackup.Dispose();
    }
}
