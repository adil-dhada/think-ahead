using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Application.Common.Pagination;
using Playbook.Domain.Activities;
using Playbook.Domain.Errors;

namespace Playbook.Application.Activities;

public sealed class GetActivityByIdHandler(
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<Activity?> Handle(string activityId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        if (!ObjectId.TryParse(activityId, out var id))
        {
            return null;
        }
        return await activities.GetByIdAsync(userId, id, ct);
    }
}

public sealed record ListActivitiesQuery(
    int First,
    string? After,
    ActivityFilter? Filter,
    ActivitySort Sort);

public sealed class ListActivitiesHandler(
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<CursorPage<Activity>> Handle(ListActivitiesQuery q, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var first = Math.Clamp(q.First, 1, 100);
        var filter = q.Filter ?? new ActivityFilter();
        return await activities.ListAsync(userId, filter, q.Sort, first, q.After, ct);
    }
}

public sealed class RecentlyViewedHandler(
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<Activity>> Handle(int limit, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        return await activities.RecentlyViewedAsync(userId, Math.Clamp(limit, 1, 50), ct);
    }
}

public sealed class FavoritesHandler(
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<Activity>> Handle(int limit, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        return await activities.FavoritesAsync(userId, Math.Clamp(limit, 1, 50), ct);
    }
}
