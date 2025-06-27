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
}