using HotChocolate.Authorization;
using HotChocolate.Types;
using MongoDB.Bson;
using Playbook.Application.Activities;
using Playbook.Application.Common.Abstractions;
using Playbook.Application.Dashboard;
using Playbook.Application.Tags;
using Playbook.Domain.Categories;

namespace Playbook.Api.GraphQL.Activities;

[ExtendObjectType(OperationTypeNames.Query)]
public sealed class ActivityQueries
{
    [Authorize]
    public async Task<ActivityConnectionNode> ActivitiesAsync(
        [Service] ListActivitiesHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        int first = 24,
        string? after = null,
        ActivityFilterInput? filter = null,
        ActivitySort sort = ActivitySort.UpdatedDesc,
        CancellationToken ct = default)
    {
        var page = await handler.Handle(new ListActivitiesQuery(first, after, filter?.ToDomain(), sort), ct);
        var catMap = await BuildCategoryMapAsync(categoryRepo, currentUser, page.Items.Select(a => a.CategoryId), ct);
        return ActivityMapper.ToConnection(page, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode?> ActivityAsync(
        string id,
        [Service] GetActivityByIdHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        CancellationToken ct = default)
    {
        var activity = await handler.Handle(id, ct);
        if (activity is null) return null;
        var catMap = await BuildCategoryMapAsync(categoryRepo, currentUser, [activity.CategoryId], ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<List<ActivityNode>> RecentlyViewedAsync(
        [Service] RecentlyViewedHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        int limit = 6,
        CancellationToken ct = default)
    {
        var items = await handler.Handle(limit, ct);
        var catMap = await BuildCategoryMapAsync(categoryRepo, currentUser, items.Select(a => a.CategoryId), ct);
        return items.Select(a => ActivityMapper.ToNode(a, catMap, att => Sas(blobs, att, ct))).ToList();
    }

    [Authorize]
    public async Task<List<ActivityNode>> FavoritesAsync(
        [Service] FavoritesHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        int limit = 6,
        CancellationToken ct = default)
    {
        var items = await handler.Handle(limit, ct);
        var catMap = await BuildCategoryMapAsync(categoryRepo, currentUser, items.Select(a => a.CategoryId), ct);
        return items.Select(a => ActivityMapper.ToNode(a, catMap, att => Sas(blobs, att, ct))).ToList();
    }

    [Authorize]
    public async Task<List<CategoryNode>> CategoriesAsync(
        [Service] Application.Categories.ListCategoriesHandler handler,
        [Service] IActivityRepository activities,
        [Service] ICurrentUser currentUser,
        CancellationToken ct = default)
    {
        var cats = await handler.Handle(ct);
        var ids = cats.Select(c => c.Id).ToList();
        var counts = await activities.CountByCategoryAsync(currentUser.RequireUserId(), ids, ct);
        return cats.Select(c => ActivityMapper.ToNode(c, counts.TryGetValue(c.Id, out var n) ? n : 0)).ToList();
    }

    [Authorize]
    public async Task<List<TagSummaryNode>> TagsAsync(
        [Service] ListTagsHandler handler,
        CancellationToken ct = default)
    {
        var tags = await handler.Handle(ct);
        return tags.Select(t => new TagSummaryNode(t.Name, t.Count)).ToList();
    }

    [Authorize]
    public async Task<List<ActivityNode>> StaleActivitiesAsync(
        [Service] IActivityRepository activityRepo,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        int staleDays = 30,
        int limit = 6,
        CancellationToken ct = default)
    {
        var userId = currentUser.RequireUserId();
        var items = await activityRepo.GetStaleActivitiesAsync(userId, staleDays, limit, ct);
        var catMap = await BuildCategoryMapAsync(categoryRepo, currentUser, items.Select(a => a.CategoryId), ct);
        return items.Select(a => ActivityMapper.ToNode(a, catMap, att => Sas(blobs, att, ct))).ToList();
    }

    [Authorize]
    public async Task<DashboardStatsNode> DashboardAsync(
        [Service] GetDashboardHandler handler,
        [Service] IActivityRepository activityRepo,
        [Service] ICurrentUser currentUser,
        CancellationToken ct = default)
    {
        var stats = await handler.Handle(ct);
        var userId = currentUser.RequireUserId();
        var staleCount = (await activityRepo.GetStaleActivitiesAsync(userId, 30, int.MaxValue, ct)).Count;
        return new DashboardStatsNode(stats.TotalActivities, stats.TotalCategories, stats.TotalTags, stats.ArchivedCount, stats.FavoritesCount, staleCount);
    }

    private static string Sas(IBlobStore blobs, Domain.Activities.AttachmentRef att, CancellationToken ct) =>
        blobs.GetReadSasUrlAsync(att.BlobPath, TimeSpan.FromMinutes(15), ct).GetAwaiter().GetResult().ToString();

    private static async Task<IReadOnlyDictionary<ObjectId, Category>> BuildCategoryMapAsync(
        ICategoryRepository categoryRepo,
        ICurrentUser currentUser,
        IEnumerable<ObjectId?> categoryIds,
        CancellationToken ct)
    {
        var ids = categoryIds.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<ObjectId, Category>();
        var cats = await categoryRepo.GetByIdsAsync(currentUser.RequireUserId(), ids, ct);
        return cats.ToDictionary(c => c.Id);
    }
}
