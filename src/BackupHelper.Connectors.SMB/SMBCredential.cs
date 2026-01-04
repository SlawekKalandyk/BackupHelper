using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.SMB;

public record SMBCredential(string Server, string ShareName, string Username, string Password)
    : CredentialBase<SMBCredentialTitle>(new SMBCredentialTitle(Server, ShareName))
{
    public const string CredentialKind = "smb";

    public SMBShareInfo GetSMBShareInfo() => new SMBShareInfo(Server, ShareName);

    protected override string GetUsername() => Username;

    protected override string GetPassword() => Password;
}

public record SMBCredentialTitle(string Server, string ShareName) : CredentialTitleBase
{
    public SMBCredentialTitle(SMBShareInfo shareInfo)
        : this(shareInfo.ServerIPAddress.ToString(), shareInfo.ShareName) { }

    public override string Kind => SMBCredential.CredentialKind;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [
            NameValuePairHelper.ToNameValuePair(Server),
            NameValuePairHelper.ToNameValuePair(ShareName),
        ];
}