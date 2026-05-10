using Playbook.Application.Common.Abstractions;

namespace Playbook.Application.Tags;

public sealed record TagSummary(string Name, int Count);

public sealed class ListTagsHandler(
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<TagSummary>> Handle(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var buckets = await activities.GetTagBucketsAsync(userId, ct);
        return buckets
            .Select(b => new TagSummary(b.Name, b.Count))
            .OrderByDescending(t => t.Count)
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
    }
}
