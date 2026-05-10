using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Playbook.Application.Common.Abstractions;

namespace Playbook.Api.Rest;

[ApiController]
[Route("api")]
[Authorize]
public sealed class UploadsController(IBlobStore blobStore, ICurrentUser currentUser) : ControllerBase
{
    private const long MaxBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        "text/plain",
        "text/csv"
    };

    [HttpPost("uploads")]
    [EnableRateLimiting("uploads")]
    [RequestSizeLimit(MaxBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxBytes)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] string? activityId, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { error = "No file provided." });

        if (file.Length > MaxBytes)
            return BadRequest(new { error = "File exceeds the 25 MB limit." });

        var declaredType = (file.ContentType ?? string.Empty).ToLowerInvariant();
        if (!AllowedContentTypes.Contains(declaredType))
            return BadRequest(new { error = $"Content type '{declaredType}' is not allowed." });

        if (declaredType.StartsWith("image/") && !await PassesMagicBytesCheckAsync(file, ct))
            return BadRequest(new { error = "File content does not match declared image type." });

        var userId = currentUser.UserId?.ToString()
            ?? throw new InvalidOperationException("Authenticated user has no id claim.");

        var segment = string.IsNullOrWhiteSpace(activityId) ? "pending" : activityId.Trim();
        var safeName = SanitizeFileName(file.FileName);
        var blobPath = $"{userId}/{segment}/{Guid.NewGuid():N}-{safeName}";

        await using var stream = file.OpenReadStream();
        var blob = await blobStore.UploadAsync(blobPath, stream, declaredType, file.FileName, ct);

        return Ok(new
        {
            blobPath = blob.BlobPath,
            fileName = blob.FileName,
            contentType = blob.ContentType,
            sizeBytes = blob.SizeBytes
        });
    }

    private static async Task<bool> PassesMagicBytesCheckAsync(IFormFile file, CancellationToken ct)
    {
        var header = new byte[12];
        await using var s = file.OpenReadStream();
        var n = await s.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (n < 3) return false;

        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        // PNG: 89 50 4E 47
        if (n >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        // GIF: 47 49 46 38
        if (n >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;
        // WebP: RIFF....WEBP
        if (n >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return true;
        // BMP: 42 4D
        if (header[0] == 0x42 && header[1] == 0x4D) return true;

        return false;
    }

    private static string SanitizeFileName(string raw)
    {
        var name = System.IO.Path.GetFileName(raw);
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        return string.Concat(name.Select(c => Array.IndexOf(invalid, c) >= 0 ? '_' : c));
    }
}
