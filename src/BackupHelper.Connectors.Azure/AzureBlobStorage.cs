using Azure;
using Azure.Storage.Blobs;

namespace BackupHelper.Connectors.Azure;

public class AzureBlobStorage
{
    private readonly string _accountName;
    private readonly BlobServiceClient _blobServiceClient;

    public AzureBlobStorage(AzureBlobCredential credential)
        : this(credential.AccountName, credential.SharedAccessSignature.Expose()) { }

    public AzureBlobStorage(string accountName, string sharedAccessSignature)
    {
        _accountName = accountName;
        _blobServiceClient = CreateBlobServiceClient(accountName, sharedAccessSignature);
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
                $"The specified container '{containerName}' does not exist at the Azure Blob Storage account '{_accountName}'."
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

    public async Task<bool> BlobExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var blobExists = await blobClient.ExistsAsync(cancellationToken);

        if (!blobExists.HasValue)
        {
            throw new InvalidOperationException("Failed to determine if blob exists.");
        }

        return blobExists.Value;
    }

    public async Task<string?> DownloadBlobTextAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        var blobExists = await blobClient.ExistsAsync(cancellationToken);
        if (!blobExists.Value)
        {
            return null;
        }

        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToString();
    }

    public async Task UploadBlobTextAsync(
        string containerName,
        string blobName,
        string content,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);
        var uploadResponse = await blobClient.UploadAsync(
            BinaryData.FromString(content),
            overwrite: true,
            cancellationToken
        );

        var status = uploadResponse.GetRawResponse().Status;

        if (status < 200 || status >= 300)
        {
            throw new InvalidOperationException(
                $"Failed to upload blob '{blobName}' to container '{containerName}'. HTTP Status: {status}."
            );
        }
    }

    public async Task DeleteBlobIfExistsAsync(
        string containerName,
        string blobName,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobName);

        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetBlobNamesAsync(
        string containerName,
        CancellationToken cancellationToken = default
    )
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobNames = new List<string>();

        await foreach (
            var blobItem in containerClient.GetBlobsAsync(cancellationToken: cancellationToken)
        )
        {
            blobNames.Add(blobItem.Name);
        }

        return blobNames;
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

    private BlobServiceClient CreateBlobServiceClient(
        string accountName,
        string sharedAccessSignature
    )
    {
        var blobServiceUri = new Uri(GetBlobServiceEndpoint(accountName));
        var azureSasCredential = new AzureSasCredential(sharedAccessSignature);
        var blobClientOptions = new BlobClientOptions()
        {
            GeoRedundantSecondaryUri = new Uri(GetBlobServiceSecondaryEndpoint(accountName)),
        };
        return new BlobServiceClient(blobServiceUri, azureSasCredential, blobClientOptions);
    }

    private string GetBlobServiceEndpoint(string accountName) =>
        $"https://{accountName}.blob.core.windows.net";

    private string GetBlobServiceSecondaryEndpoint(string accountName) =>
        $"https://{accountName}-secondary.blob.core.windows.net";
}