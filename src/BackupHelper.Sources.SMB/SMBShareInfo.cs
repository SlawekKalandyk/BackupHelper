using System.Net;

namespace BackupHelper.Sources.SMB;

// Sample SMB path: \\192.168.0.1\shareName\a\b\c.
public record SMBShareInfo(IPAddress ServerIPAddress, string ShareName)
{
    public static SMBShareInfo FromFilePath(string path)
    {
        var trimmed = path.TrimStart('\\');
        var parts = trimmed.Split('\\');

        if (parts.Length < 2)
            throw new ArgumentException("Invalid SMB path format", nameof(path));

        var serverIp = IPAddress.Parse(parts[0]);
        var shareName = parts[1];
        return new SMBShareInfo(serverIp, shareName);
    }

    public override string ToString()
    {
        return $@"\\{ServerIPAddress.ToString()}\{ShareName}";
    }
}