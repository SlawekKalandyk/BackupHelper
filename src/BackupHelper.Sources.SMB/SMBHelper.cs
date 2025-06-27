namespace BackupHelper.Sources.SMB;

public static class SMBHelper
{
    public static string StripShareInfo(string path)
    {
        var shareInfo = SMBShareInfo.FromFilePath(path);
        return path.Substring(shareInfo.ToString().Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}