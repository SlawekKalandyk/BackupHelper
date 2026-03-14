using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.Azure;

[System.Diagnostics.DebuggerDisplay("AzureBlobCredential {{ AccountName = {AccountName} }}")]
public record AzureBlobCredential(string AccountName, SensitiveString SharedAccessSignature)
    : CredentialBase<AzureBlobCredentialTitle>(new AzureBlobCredentialTitle(AccountName))
{
    public const string CredentialKind = "azure";

    protected override string GetUsername() => AccountName;

    protected override SensitiveString GetPassword() => SharedAccessSignature;

    public override string ToString() => $"AzureBlobCredential {{ AccountName = {AccountName} }}";
}

public record AzureBlobCredentialTitle(string AccountName) : CredentialTitleBase
{
    public override string Kind => AzureBlobCredential.CredentialKind;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [NameValuePairHelper.ToNameValuePair(AccountName)];
}
