namespace BackupHelper.Sources.SMB.IO;

[Flags]
public enum FilePurpose
{
    Read = 1,
    Write = 2,
    Delete = 4,
}