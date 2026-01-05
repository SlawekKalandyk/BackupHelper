using System.Net;

namespace BackupHelper.Connectors.SMB;

// Sample SMB path: \\192.168.0.1\shareName\a\b\c.
public record SMBShareInfo
{
    public SMBShareInfo(IPAddress serverIPAddress, string shareName)
    {
        ServerIPAddress = serverIPAddress;
        ShareName = shareName;
    }

    public SMBShareInfo(string serverIPAddressString, string shareName)
        : this(IPAddress.Parse(serverIPAddressString), shareName) { }

    public IPAddress ServerIPAddress { get; }
    public string ShareName { get; }

    public static SMBShareInfo FromSMBPath(string path)
    {
        var trimmed = path.TrimStart('\\');
        var parts = trimmed.Split('\\');

        if (parts.Length < 2)
            throw new ArgumentException("Invalid SMB path format", nameof(path));

        var serverIp = IPAddress.Parse(parts[0]);
        var shareName = parts[1];
        return new SMBShareInfo(serverIp, shareName);
    }

    public override string ToString() => $@"\\{ServerIPAddress.ToString()}\{ShareName}";
}