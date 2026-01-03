using BackupHelper.Abstractions;
using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.SMB;

public record SMBCredential(string Server, string ShareName, string Username, string Password)
    : CredentialBase
{
    public const string CredentialType = "smb";
    public override string Kind => CredentialType;

    public SMBShareInfo GetSMBShareInfo() => SMBShareInfo.FromSMBPath(GetLocalTitle());

    protected override string GetLocalTitle() =>
        SMBCredentialHelper.GetSMBCredentialTitle(Server, ShareName);

    protected override string GetUsername() => Username;

    protected override string GetPassword() => Password;
}