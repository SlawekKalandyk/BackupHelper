using Azure;
using Azure.Storage.Blobs;

namespace BackupHelper.Connectors.Azure;

public class AzureBlobStorage
{
    private readonly AzureBlobCredential _credential;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorage(AzureBlobCredential credential)
    {
        _credential = credential;
        _blobServiceClient = CreateBlobServiceClient(credential);
    }

    // TODO: Properly handle exceptions, return some kind of result object instead of throwing.
    public async Task UploadBlobAsync(
        string containerName,
        string blobName,
        Stream data,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var isContainerAvailable = await IsContainerAvailableAsync(
            containerName,
            cancellationToken
        );

        if (!isContainerAvailable)
        {
            throw new InvalidOperationException(
                $"The specified container '{containerName}' does not exist at the Azure Blob Storage account '{_credential.AccountName}'."
            );
        }

        var blobClient = containerClient.GetBlobClient(blobName);
        var uploadResponse = await blobClient.UploadAsync(data, overwrite: true, cancellationToken);

        var status = uploadResponse.GetRawResponse().Status;

        if (status < 200 || status >= 300)
        {
            throw new InvalidOperationException(
                $"Failed to upload blob '{blobName}' to container '{containerName}'. HTTP Status: {status}."
            );
        }

        var uploadedBlobExistsResponse = await blobClient.ExistsAsync(cancellationToken);

        if (!uploadedBlobExistsResponse.Value)
        {
            throw new InvalidOperationException("Upload returned success but blob does not exist.");
        }
    }

    public async Task<bool> IsContainerAvailableAsync(
        string containerName,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var containerExists = await containerClient.ExistsAsync(cancellationToken);

        if (!containerExists.HasValue)
        {
            throw new InvalidOperationException("Failed to determine if container exists.");
        }

        return containerExists.Value;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _blobServiceClient.GetAccountInfoAsync(cancellationToken);
            var status = response.GetRawResponse().Status;
            return status >= 200 && status < 300;
        }
        catch
        {
            return false;
        }
    }

    private BlobServiceClient CreateBlobServiceClient(AzureBlobCredential credential)
    {
        var blobServiceUri = new Uri(GetBlobServiceEndpoint(credential));
        var azureSasCredential = new AzureSasCredential(credential.SharedAccessSignature);
        var blobClientOptions = new BlobClientOptions()
        {
            GeoRedundantSecondaryUri = new Uri(GetBlobServiceSecondaryEndpoint(credential)),
        };
        return new BlobServiceClient(blobServiceUri, azureSasCredential, blobClientOptions);
    }

    private string GetBlobServiceEndpoint(AzureBlobCredential credential) =>
        $"https://{credential.AccountName}.blob.core.windows.net";

    private string GetBlobServiceSecondaryEndpoint(AzureBlobCredential credential) =>
        $"https://{credential.AccountName}-secondary.blob.core.windows.net";
}