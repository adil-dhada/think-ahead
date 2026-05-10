using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Playbook.Application.Common.Abstractions;

namespace Playbook.Infrastructure.Storage;

public sealed class BlobOptions
{
    public const string Section = "Blob";
    public string ConnectionString { get; set; } = string.Empty;
    public string Container { get; set; } = "pb-files";
}

public sealed class AzureBlobStore(IOptions<BlobOptions> options) : IBlobStore
{
    private readonly BlobOptions _opts = options.Value;
    private BlobContainerClient GetContainer() => new BlobServiceClient(_opts.ConnectionString)
        .GetBlobContainerClient(_opts.Container);

    public async Task<UploadedBlob> UploadAsync(string blobPath, Stream content, string contentType, string fileName, CancellationToken ct)
    {
        var container = GetContainer();
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = container.GetBlobClient(blobPath);
        await blob.UploadAsync(content, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        }, ct);
        var props = await blob.GetPropertiesAsync(cancellationToken: ct);
        return new UploadedBlob(blobPath, fileName, contentType, props.Value.ContentLength);
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct)
    {
        var blob = GetContainer().GetBlobClient(blobPath);
        await blob.DeleteIfExistsAsync(cancellationToken: ct);
    }

    public async Task<Uri> GetReadSasUrlAsync(string blobPath, TimeSpan ttl, CancellationToken ct)
    {
        var container = GetContainer();
        var blob = container.GetBlobClient(blobPath);

        // BlobSasBuilder approach works with connection-string-based clients (storage account key available).
        // For production (user-delegation SAS), swap to BlobServiceClient with DefaultAzureCredential.
        var sasBuilder = new BlobSasBuilder(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(ttl))
        {
            BlobContainerName = _opts.Container,
            BlobName = blobPath,
            Resource = "b"
        };
        return blob.GenerateSasUri(sasBuilder);
    }

    public async Task<bool> ExistsAsync(string blobPath, CancellationToken ct)
    {
        var blob = GetContainer().GetBlobClient(blobPath);
        var response = await blob.ExistsAsync(ct);
        return response.Value;
    }
}
