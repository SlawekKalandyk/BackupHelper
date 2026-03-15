using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.SMB;

[System.Diagnostics.DebuggerDisplay(
    "SMBCredential {{ Server = {Server}, ShareName = {ShareName}, Username = {Username} }}"
)]
public record SMBCredential(
    string Server,
    string ShareName,
    string Username,
    SensitiveString Password
) : CredentialBase<SMBCredentialTitle>(new SMBCredentialTitle(Server, ShareName), Password)
{
    public const string CredentialKind = "smb";

    public SMBShareInfo GetSMBShareInfo() => new SMBShareInfo(Server, ShareName);

    protected override string GetUsername() => Username;

    public override string ToString() =>
        $"SMBCredential {{ Server = {Server}, ShareName = {ShareName}, Username = {Username} }}";
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
