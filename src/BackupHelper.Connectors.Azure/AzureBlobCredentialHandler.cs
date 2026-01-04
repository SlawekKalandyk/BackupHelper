using BackupHelper.Abstractions.Credentials;

namespace BackupHelper.Connectors.Azure;

public class AzureBlobCredentialHandler : CredentialHandlerBase<AzureBlobCredential>
{
    public override string Kind => AzureBlobCredential.CredentialKind;

    protected override AzureBlobCredential FromCredentialEntryCore(
        CredentialEntry entry,
        string localTitle
    ) => new(entry.Username, entry.Password);

    protected override async Task<bool> TestConnectionAsyncCore(
        AzureBlobCredential credential,
        CancellationToken cancellationToken
    )
    {
        var azureBlobStorage = new AzureBlobStorage(credential);
        return await azureBlobStorage.TestConnectionAsync(cancellationToken);
    }
}
