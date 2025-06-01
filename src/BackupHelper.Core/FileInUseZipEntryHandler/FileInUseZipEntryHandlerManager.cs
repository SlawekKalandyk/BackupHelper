using System.Security.Principal;

namespace BackupHelper.Core.FileInUseZipEntryHandler;

public interface IFileInUseZipEntryHandlerManager : IDisposable
{
    IFileInUseZipEntryHandler GetFileInUseZipEntryHandler(string filepath);
}

public class FileInUseZipEntryHandlerManager : IFileInUseZipEntryHandlerManager
{
    private readonly VssFileInUseZipEntryHandlerFactory _vssFileInUseZipEntryHandlerFactory;
    private IDictionary<string, IFileInUseZipEntryHandler> _handlers = new Dictionary<string, IFileInUseZipEntryHandler>();

    public FileInUseZipEntryHandlerManager(VssFileInUseZipEntryHandlerFactory vssFileInUseZipEntryHandlerFactory)
    {
        _vssFileInUseZipEntryHandlerFactory = vssFileInUseZipEntryHandlerFactory;
    }

    public IFileInUseZipEntryHandler GetFileInUseZipEntryHandler(string filepath)
    {
        var absolutePath = Path.GetFullPath(filepath);
        var volume = Path.GetPathRoot(absolutePath)!;

        if (_handlers.TryGetValue(volume, out var handler))
        {
            return handler;
        }

        var driveInfo = new DriveInfo(volume);
        var driveFormat = driveInfo.DriveFormat?.ToLowerInvariant();

        if (OperatingSystem.IsWindows())
        {
            #pragma warning disable CA1416
            var hasWindowsAdminRights = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);
            #pragma warning restore CA1416

            switch ((hasWindowsAdminRights, driveFormat))
            {
                case (true, "ntfs"):
                    handler = _vssFileInUseZipEntryHandlerFactory.Create();
                    _handlers[volume] = handler;
                    return handler;
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
        foreach (var handler in _handlers.Values)
        {
            handler.Dispose();
        }
    }
}