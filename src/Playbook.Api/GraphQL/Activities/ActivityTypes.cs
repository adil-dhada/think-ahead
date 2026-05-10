using MongoDB.Bson;
using Playbook.Application.Common.Pagination;
using Playbook.Domain.Activities;
using Playbook.Domain.Categories;

namespace Playbook.Api.GraphQL.Activities;

public sealed record AttachmentNode(
    string BlobPath,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTime UploadedAt,
    string DownloadUrl);

public sealed record ActivityNode(
    string Id,
    string UserId,
    string Title,
    string? Description,
    string? Notes,
    CategoryNode? Category,
    List<string> Tags,
    List<string> Dos,
    List<string> Donts,
    List<AttachmentNode> Attachments,
    bool IsFavorite,
    bool IsArchived,
    int ViewCount,
    DateTime? LastViewedAt,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CategoryNode(
    string Id,
    string Name,
    string Color,
    int ActivityCount);

public sealed record TagSummaryNode(string Name, int Count);

public sealed record DashboardStatsNode(
    int TotalActivities,
    int TotalCategories,
    int TotalTags,
    int ArchivedCount,
    int FavoritesCount);

public sealed record PageInfoNode(string? EndCursor, bool HasNextPage);

public sealed record ActivityConnectionNode(
    List<ActivityNode> Nodes,
    PageInfoNode PageInfo);

public static class ActivityMapper
{
    public static ActivityNode ToNode(
        Activity a,
        IReadOnlyDictionary<ObjectId, Category>? categoryMap,
        Func<AttachmentRef, string> sasResolver)
    {
        Category? cat = a.CategoryId.HasValue && categoryMap is not null
            ? categoryMap.GetValueOrDefault(a.CategoryId.Value)
            : null;
        return new(
            a.Id.ToString(),
            a.UserId.ToString(),
            a.Title,
            a.DescriptionJson,
            a.NotesJson,
            cat is null ? null : new CategoryNode(cat.Id.ToString(), cat.Name, cat.Color.ToString(), 0),
            a.Tags,
            a.Dos,
            a.Donts,
            a.Attachments.Select(att => new AttachmentNode(
                att.BlobPath, att.FileName, att.ContentType, att.SizeBytes, att.UploadedAt,
                sasResolver(att))).ToList(),
            a.IsFavorite,
            a.IsArchived,
            a.ViewCount,
            a.LastViewedAt,
            a.CreatedAt,
            a.UpdatedAt);
    }

    public static ActivityConnectionNode ToConnection(
        CursorPage<Activity> page,
        IReadOnlyDictionary<ObjectId, Category>? categoryMap,
        Func<AttachmentRef, string> sasResolver) => new(
        page.Items.Select(a => ToNode(a, categoryMap, sasResolver)).ToList(),
        new PageInfoNode(page.EndCursor, page.HasNextPage));

    public static CategoryNode ToNode(Category c, int count) => new(
        c.Id.ToString(), c.Name, c.Color.ToString(), count);
}
