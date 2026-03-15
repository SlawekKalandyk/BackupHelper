using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.Azure;

[System.Diagnostics.DebuggerDisplay("AzureBlobCredential {{ AccountName = {AccountName} }}")]
public record AzureBlobCredential : CredentialBase<AzureBlobCredentialTitle>
{
    public AzureBlobCredential(string accountName, SensitiveString sharedAccessSignature)
        : base(new AzureBlobCredentialTitle(accountName), sharedAccessSignature)
    {
        AccountName = accountName;
    }

    public const string CredentialKind = "azure";

    public string AccountName { get; }
    public SensitiveString SharedAccessSignature => CredentialPassword;

    protected override string GetUsername() => AccountName;

    public override string ToString() => $"AzureBlobCredential {{ AccountName = {AccountName} }}";
}

public record AzureBlobCredentialTitle(string AccountName) : CredentialTitleBase
{
    public override string Kind => AzureBlobCredential.CredentialKind;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [NameValuePairHelper.ToNameValuePair(AccountName)];
}
