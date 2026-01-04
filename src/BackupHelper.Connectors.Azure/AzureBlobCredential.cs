using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.Azure;

public record AzureBlobCredential(string AccountName, string SharedAccessSignature)
    : CredentialBase<AzureBlobCredentialTitle>(new AzureBlobCredentialTitle(AccountName))
{
    public const string CredentialKind = "azure";

    protected override string GetUsername() => AccountName;

    protected override string GetPassword() => SharedAccessSignature;
}

public record AzureBlobCredentialTitle(string AccountName) : CredentialTitleBase
{
    public override string Kind => AzureBlobCredential.CredentialKind;

    public override IEnumerable<KeyValuePair<string, string>> GetTitleComponents() =>
        [NameValuePairHelper.ToNameValuePair(AccountName)];
}