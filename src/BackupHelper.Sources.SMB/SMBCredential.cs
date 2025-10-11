using BackupHelper.Abstractions;

namespace BackupHelper.Sources.SMB;

public record SMBCredential(string Server, string ShareName, string Username, string Password)
{
    public CredentialEntry ToCredentialEntry()
    {
        var title = SMBCredentialHelper.GetSMBCredentialTitle(Server, ShareName);
        return new CredentialEntry(title, Username, Password);
    }
}
