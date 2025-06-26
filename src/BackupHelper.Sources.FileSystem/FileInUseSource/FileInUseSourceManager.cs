using System.Security.Principal;

namespace BackupHelper.Sources.FileSystem.FileInUseSource;

public interface IFileInUseSourceManager : IDisposable
{
    IFileInUseSource GetFileInUseSource(string filepath);
}

public class FileInUseSourceManager : IFileInUseSourceManager
{
    private readonly VssFileInUseSourceFactory _vssFileInUseSourceFactory;
    private IDictionary<string, IFileInUseSource> _sources = new Dictionary<string, IFileInUseSource>();

    public FileInUseSourceManager(VssFileInUseSourceFactory vssFileInUseSourceFactory)
    {
        _vssFileInUseSourceFactory = vssFileInUseSourceFactory;
    }

    public IFileInUseSource GetFileInUseSource(string filepath)
    {
        var absolutePath = Path.GetFullPath(filepath);
        var volume = Path.GetPathRoot(absolutePath)!;

        if (_sources.TryGetValue(volume, out var source))
        {
            return source;
        }

        var driveInfo = new DriveInfo(volume);
        var driveFormat = driveInfo.DriveFormat?.ToLowerInvariant();

        if (OperatingSystem.IsWindows())
        {
            #pragma warning disable CA1416
            var hasWindowsAdminRights = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            #pragma warning restore CA1416

            switch (hasWindowsAdminRights, driveFormat)
            {
                case (true, "ntfs"):
                    source = _vssFileInUseSourceFactory.Create();
                    _sources[volume] = source;
                    return source;
                case (false, "ntfs"):
                    throw new UnauthorizedAccessException("You need administrative rights to safely copy files in use on NTFS volumes.");
                case (_, _):
                    throw new NotSupportedException($"Unsupported file system format: {driveFormat}.");
            }
        }

        throw new NotSupportedException("Your operating system is not supported.");
    }

    public void Dispose()
    {
        foreach (var handler in _sources.Values)
        {
            handler.Dispose();
        }
    }
}