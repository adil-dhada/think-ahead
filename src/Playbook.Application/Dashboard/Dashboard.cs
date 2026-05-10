using Playbook.Application.Common.Abstractions;

namespace Playbook.Application.Dashboard;

public sealed record DashboardStats(
    int TotalActivities,
    int TotalCategories,
    int TotalTags,
    int ArchivedCount,
    int FavoritesCount);

public sealed class GetDashboardHandler(
    IActivityRepository activities,
    ICategoryRepository categories,
    ICurrentUser currentUser)
{
    public async Task<DashboardStats> Handle(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var counts = await activities.GetDashboardCountsAsync(userId, ct);
        var cats = await categories.ListAsync(userId, ct);
        return new DashboardStats(
            counts.Total,
            cats.Count,
            counts.DistinctTags,
            counts.Archived,
            counts.Favorites);
    }
}
