using SMBLibrary;

namespace BackupHelper.Sources.SMB;

internal static class SMBHelper
{
    public static string StripShareInfo(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        return path.Substring(shareInfo.ToString().Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static void ThrowIfStatusNotSuccess(NTStatus status, string operation)
    {
        if (status != NTStatus.STATUS_SUCCESS)
        {
            throw new InvalidOperationException($"SMB operation '{operation}' failed with status: {status}");
        }
    }

    public static void ThrowIfStatusNotNoMoreFiles(NTStatus status, string operation)
    {
        if (status != NTStatus.STATUS_NO_MORE_FILES)
        {
            throw new InvalidOperationException($"SMB operation '{operation}' failed with status: {status}");
        }
    }

    public static void ThrowIfFileStatusNotFileOpened(FileStatus fileStatus, string path, string operation)
    {
        if ( fileStatus != FileStatus.FILE_OPENED)
        {
            throw new InvalidOperationException($"SMB operation '{operation}' failed to open '{path}' with file status: {fileStatus}");
        }
    }

    public static void ThrowIfFileStatusNotFileCreated(FileStatus fileStatus, string path, string operation)
    {
        if (fileStatus != FileStatus.FILE_CREATED)
        {
            throw new InvalidOperationException($"SMB operation '{operation}' failed to create '{path}' with file status: {fileStatus}");
        }
    }
}