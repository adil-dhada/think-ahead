namespace Playbook.Application.Common.Abstractions;

public sealed record UploadedBlob(string BlobPath, string FileName, string ContentType, long SizeBytes);

public interface IBlobStore
{
    Task<UploadedBlob> UploadAsync(string blobPath, Stream content, string contentType, string fileName, CancellationToken ct);
    Task DeleteAsync(string blobPath, CancellationToken ct);
    Task<Uri> GetReadSasUrlAsync(string blobPath, TimeSpan ttl, CancellationToken ct);
    Task<bool> ExistsAsync(string blobPath, CancellationToken ct);
}
