using MongoDB.Bson.Serialization.Attributes;

namespace Playbook.Domain.Activities;

[BsonIgnoreExtraElements]
public sealed class AttachmentRef
{
    public string BlobPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }

    public AttachmentRef() { }

    public AttachmentRef(string blobPath, string fileName, string contentType, long sizeBytes, DateTime uploadedAt)
    {
        BlobPath = blobPath;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedAt = uploadedAt;
    }
}
