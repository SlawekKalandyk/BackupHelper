using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.Azure;

public record AzureBlobCredential(string AccountName, string SharedAccessSignature) : CredentialBase
{
    public const string CredentialKind = "azure";
    public override string Kind => CredentialKind;

    protected override string GetLocalTitle() => AccountName;

    protected override string GetUsername() => AccountName;

    protected override string GetPassword() => SharedAccessSignature;
}