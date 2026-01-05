using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.Azure;

public class AzureBlobCredentialHandler : CredentialHandlerBase<AzureBlobCredential>
{
    public override string Kind => AzureBlobCredential.CredentialKind;

    public override AzureBlobCredential FromCredentialEntry(CredentialEntry entry) =>
        new(entry.Username, entry.Password);

    protected override async Task<bool> TestConnectionAsyncCore(
        AzureBlobCredential credential,
        CancellationToken cancellationToken
    )
    {
        var azureBlobStorage = new AzureBlobStorage(credential);
        return await azureBlobStorage.TestConnectionAsync(cancellationToken);
    }
}