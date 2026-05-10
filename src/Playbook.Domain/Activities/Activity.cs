using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Playbook.Domain.Errors;

namespace Playbook.Domain.Activities;

[BsonIgnoreExtraElements]
public sealed class Activity
{
    public const int MaxTitleLength = 200;
    public const int MaxDosOrDontsItems = 50;
    public const int MaxItemLength = 500;
    public const int MaxAttachments = 25;
    public const int MaxTags = 30;

    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string Title { get; set; } = string.Empty;

    public string? DescriptionJson { get; set; }
    public string? NotesJson { get; set; }

    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public ObjectId? CategoryId { get; set; }

    public List<string> Tags { get; set; } = new();
    public List<string> Dos { get; set; } = new();
    public List<string> Donts { get; set; } = new();
    public List<AttachmentRef> Attachments { get; set; } = new();

    public bool IsFavorite { get; set; }
    public bool IsArchived { get; set; }
    public int ViewCount { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public List<ActivityRun> Runs { get; set; } = [];
    public int RunCount => Runs.Count;
    public DateTime? LastRunAt => Runs.Count > 0 ? Runs[^1].ExecutedAt : null;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int SchemaVersion { get; set; } = 1;

    public Activity() { }

    public static Activity Create(
        ObjectId userId,
        string title,
        string? descriptionJson,
        string? notesJson,
        ObjectId? categoryId,
        IEnumerable<string>? tags,
        IEnumerable<string>? dos,
        IEnumerable<string>? donts,
        DateTime nowUtc) => new()
    {
        Id = ObjectId.GenerateNewId(),
        UserId = userId,
        Title = NormalizeTitle(title),
        DescriptionJson = descriptionJson,
        NotesJson = notesJson,
        CategoryId = categoryId,
        Tags = NormalizeTags(tags),
        Dos = NormalizeList(dos, nameof(Dos)),
        Donts = NormalizeList(donts, nameof(Donts)),
        CreatedAt = nowUtc,
        UpdatedAt = nowUtc
    };

    public void Update(
        string title,
        string? descriptionJson,
        string? notesJson,
        ObjectId? categoryId,
        IEnumerable<string>? tags,
        IEnumerable<string>? dos,
        IEnumerable<string>? donts,
        DateTime nowUtc)
    {
        Title = NormalizeTitle(title);
        DescriptionJson = descriptionJson;
        NotesJson = notesJson;
        CategoryId = categoryId;
        Tags = NormalizeTags(tags);
        Dos = NormalizeList(dos, nameof(Dos));
        Donts = NormalizeList(donts, nameof(Donts));
        UpdatedAt = nowUtc;
    }

    public void Archive(bool archived, DateTime nowUtc)
    {
        if (IsArchived == archived) return;
        IsArchived = archived;
        UpdatedAt = nowUtc;
    }

    public void ToggleFavorite(DateTime nowUtc)
    {
        IsFavorite = !IsFavorite;
        UpdatedAt = nowUtc;
    }

    public void RecordView(DateTime nowUtc)
    {
        ViewCount++;
        LastViewedAt = nowUtc;
    }

    public void RecordRun(string? outcomeNote, DateTime nowUtc)
    {
        Runs.Add(new ActivityRun { ExecutedAt = nowUtc, OutcomeNote = outcomeNote });
        UpdatedAt = nowUtc;
    }

    public void AttachFile(AttachmentRef attachment, DateTime nowUtc)
    {
        if (Attachments.Count >= MaxAttachments)
            throw new DomainValidationException($"An activity can have at most {MaxAttachments} attachments.");
        if (Attachments.Any(a => a.BlobPath == attachment.BlobPath))
            throw new ConflictException("ATTACHMENT_EXISTS", "Attachment already linked to this activity.");
        Attachments.Add(attachment);
        UpdatedAt = nowUtc;
    }

    public void DetachFile(string blobPath, DateTime nowUtc)
    {
        var existing = Attachments.FirstOrDefault(a => a.BlobPath == blobPath)
            ?? throw new NotFoundException("Attachment", blobPath);
        Attachments.Remove(existing);
        UpdatedAt = nowUtc;
    }

    public void ClearCategory(DateTime nowUtc)
    {
        if (CategoryId is null) return;
        CategoryId = null;
        UpdatedAt = nowUtc;
    }

    private static string NormalizeTitle(string title)
    {
        var trimmed = (title ?? string.Empty).Trim();
        if (trimmed.Length == 0) throw new DomainValidationException("Activity title cannot be empty.");
        if (trimmed.Length > MaxTitleLength) throw new DomainValidationException($"Activity title cannot exceed {MaxTitleLength} characters.");
        return trimmed;
    }

    private static List<string> NormalizeTags(IEnumerable<string>? tags)
    {
        if (tags is null) return new();
        var normalized = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().TrimStart('#').ToLowerInvariant())
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalized.Count > MaxTags) throw new DomainValidationException($"An activity can have at most {MaxTags} tags.");
        return normalized;
    }

    private static List<string> NormalizeList(IEnumerable<string>? items, string field)
    {
        if (items is null) return new();
        var list = items.Where(i => !string.IsNullOrWhiteSpace(i)).Select(i => i.Trim()).ToList();
        if (list.Count > MaxDosOrDontsItems) throw new DomainValidationException($"{field} cannot have more than {MaxDosOrDontsItems} items.");
        if (list.Any(i => i.Length > MaxItemLength)) throw new DomainValidationException($"{field} items cannot exceed {MaxItemLength} characters.");
        return list;
    }
}

public sealed class ActivityRun
{
    public DateTime ExecutedAt { get; set; }
    public string? OutcomeNote { get; set; }
}
