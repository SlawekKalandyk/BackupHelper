using BackupHelper.Sources.Abstractions;

namespace BackupHelper.Sources.SMB;

public class SMBSource : ISource
{
    public string GetScheme() => "smb";

    public Stream GetStream(string path)
    {
        throw new NotImplementedException();
    }
}