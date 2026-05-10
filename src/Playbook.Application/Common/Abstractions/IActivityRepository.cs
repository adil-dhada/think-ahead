using MongoDB.Bson;
using Playbook.Application.Activities;
using Playbook.Application.Common.Pagination;
using Playbook.Domain.Activities;

namespace Playbook.Application.Common.Abstractions;

public interface IActivityRepository
{
    Task<Activity?> GetByIdAsync(ObjectId userId, ObjectId activityId, CancellationToken ct);
    Task<IReadOnlyList<Activity>> GetByIdsAsync(ObjectId userId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct);
    Task<CursorPage<Activity>> ListAsync(ObjectId userId, ActivityFilter filter, ActivitySort sort, int first, string? after, CancellationToken ct);
    Task<IReadOnlyList<Activity>> RecentlyViewedAsync(ObjectId userId, int limit, CancellationToken ct);
    Task<IReadOnlyList<Activity>> FavoritesAsync(ObjectId userId, int limit, CancellationToken ct);
    Task AddAsync(Activity activity, CancellationToken ct);
    Task UpdateAsync(Activity activity, CancellationToken ct);
    Task DeleteAsync(ObjectId userId, ObjectId activityId, CancellationToken ct);
    Task<int> DetachCategoryAsync(ObjectId userId, ObjectId categoryId, CancellationToken ct);
    Task<IReadOnlyDictionary<ObjectId, int>> CountByCategoryAsync(ObjectId userId, IReadOnlyCollection<ObjectId> categoryIds, CancellationToken ct);
    Task<DashboardCounts> GetDashboardCountsAsync(ObjectId userId, CancellationToken ct);
    Task<IReadOnlyList<TagBucket>> GetTagBucketsAsync(ObjectId userId, CancellationToken ct);
}

public sealed record DashboardCounts(int Total, int Archived, int Favorites, int DistinctTags);

public sealed record TagBucket(string Name, int Count);
