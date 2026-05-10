using MongoDB.Bson;

namespace Playbook.Application.Activities;

public sealed record ActivityFilter(
    ObjectId? CategoryId = null,
    IReadOnlyList<string>? Tags = null,
    string? Search = null,
    bool? FavoritesOnly = null,
    bool IncludeArchived = false);

public enum ActivitySort
{
    UpdatedDesc,
    UpdatedAsc,
    TitleAsc,
    CreatedDesc,
    LastViewedDesc
}
