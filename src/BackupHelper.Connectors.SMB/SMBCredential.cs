using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.SMB;

[System.Diagnostics.DebuggerDisplay(
    "SMBCredential {{ Server = {Server}, ShareName = {ShareName}, Username = {Username} }}"
)]
public record SMBCredential : CredentialBase<SMBCredentialTitle>
{
    public SMBCredential(string server, string shareName, string username, SensitiveString password)
        : base(new SMBCredentialTitle(server, shareName), password)
    {
        Server = server;
        ShareName = shareName;
        Username = username;
    }

    public const string CredentialKind = "smb";

    public string Server { get; }
    public string ShareName { get; }
    public string Username { get; }
    public SensitiveString Password => CredentialPassword;

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
