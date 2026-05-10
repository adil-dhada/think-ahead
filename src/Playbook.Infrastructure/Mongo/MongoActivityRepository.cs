using MongoDB.Bson;
using MongoDB.Driver;
using Playbook.Application.Activities;
using Playbook.Application.Common.Abstractions;
using Playbook.Application.Common.Pagination;
using Playbook.Domain.Activities;

namespace Playbook.Infrastructure.Mongo;

public sealed class MongoActivityRepository(IMongoDatabase db) : IActivityRepository
{
    private readonly IMongoCollection<Activity> _col = db.GetCollection<Activity>("activities");

    public Task<Activity?> GetByIdAsync(ObjectId userId, ObjectId activityId, CancellationToken ct) =>
        _col.Find(a => a.UserId == userId && a.Id == activityId).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<Activity>> GetByIdsAsync(ObjectId userId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct) =>
        await _col.Find(a => a.UserId == userId && ids.Contains(a.Id)).ToListAsync(ct);

    public async Task<CursorPage<Activity>> ListAsync(
        ObjectId userId, ActivityFilter filter, ActivitySort sort, int first, string? after, CancellationToken ct)
    {
        var baseFilter = BuildBaseFilter(userId, filter);
        var cursorFilter = BuildCursorFilter(after, sort);
        var combined = cursorFilter is null ? baseFilter : baseFilter & cursorFilter;
        var sortDef = BuildSort(sort);

        // Fetch one extra to determine hasNextPage.
        var items = await _col.Find(combined).Sort(sortDef).Limit(first + 1).ToListAsync(ct);
        var hasNext = items.Count > first;
        if (hasNext) items.RemoveAt(items.Count - 1);

        string? endCursor = items.Count == 0
            ? null
            : EncodeCursor(sort, items[^1]);

        return new CursorPage<Activity>(items, endCursor, hasNext);
    }

    public async Task<IReadOnlyList<Activity>> RecentlyViewedAsync(ObjectId userId, int limit, CancellationToken ct) =>
        await _col.Find(a => a.UserId == userId && !a.IsArchived && a.LastViewedAt != null)
            .SortByDescending(a => a.LastViewedAt)
            .Limit(limit)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Activity>> FavoritesAsync(ObjectId userId, int limit, CancellationToken ct) =>
        await _col.Find(a => a.UserId == userId && a.IsFavorite && !a.IsArchived)
            .SortByDescending(a => a.UpdatedAt)
            .Limit(limit)
            .ToListAsync(ct);

    public Task AddAsync(Activity activity, CancellationToken ct) =>
        _col.InsertOneAsync(activity, cancellationToken: ct);

    public Task UpdateAsync(Activity activity, CancellationToken ct) =>
        _col.ReplaceOneAsync(a => a.Id == activity.Id, activity, cancellationToken: ct);

    public Task DeleteAsync(ObjectId userId, ObjectId activityId, CancellationToken ct) =>
        _col.DeleteOneAsync(a => a.UserId == userId && a.Id == activityId, ct);

    public async Task<int> DetachCategoryAsync(ObjectId userId, ObjectId categoryId, CancellationToken ct)
    {
        var update = Builders<Activity>.Update.Set(a => a.CategoryId, null as ObjectId?);
        var result = await _col.UpdateManyAsync(
            a => a.UserId == userId && a.CategoryId == categoryId, update, cancellationToken: ct);
        return (int)result.ModifiedCount;
    }

    public async Task<IReadOnlyDictionary<ObjectId, int>> CountByCategoryAsync(
        ObjectId userId, IReadOnlyCollection<ObjectId> categoryIds, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "UserId", userId },
                { "IsArchived", false },
                { "CategoryId", new BsonDocument("$in", new BsonArray(categoryIds)) }
            }),
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", "$CategoryId" },
                { "count", new BsonDocument("$sum", 1) }
            })
        };

        var results = await _col.Aggregate<BsonDocument>(pipeline, cancellationToken: ct).ToListAsync(ct);
        return results.ToDictionary(
            d => d["_id"].AsObjectId,
            d => d["count"].AsInt32);
    }

    public async Task<DashboardCounts> GetDashboardCountsAsync(ObjectId userId, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument("UserId", userId)),
            new BsonDocument("$facet", new BsonDocument
            {
                { "total", new BsonArray { new BsonDocument("$count", "n") } },
                { "archived", new BsonArray { new BsonDocument("$match", new BsonDocument("IsArchived", true)), new BsonDocument("$count", "n") } },
                { "favorites", new BsonArray { new BsonDocument("$match", new BsonDocument("IsFavorite", true)), new BsonDocument("$count", "n") } },
                { "tags", new BsonArray
                    {
                        new BsonDocument("$unwind", "$Tags"),
                        new BsonDocument("$group", new BsonDocument("_id", "$Tags")),
                        new BsonDocument("$count", "n")
                    }
                }
            })
        };

        var result = await _col.Aggregate<BsonDocument>(pipeline, cancellationToken: ct).FirstOrDefaultAsync(ct);
        if (result is null) return new DashboardCounts(0, 0, 0, 0);

        int Count(string facet) =>
            result[facet].AsBsonArray.FirstOrDefault()?["n"].AsInt32 ?? 0;

        return new DashboardCounts(Count("total"), Count("archived"), Count("favorites"), Count("tags"));
    }

    public async Task<IReadOnlyList<TagBucket>> GetTagBucketsAsync(ObjectId userId, CancellationToken ct)
    {
        var pipeline = new[]
        {
            new BsonDocument("$match", new BsonDocument { { "UserId", userId }, { "IsArchived", false } }),
            new BsonDocument("$unwind", "$Tags"),
            new BsonDocument("$group", new BsonDocument { { "_id", "$Tags" }, { "count", new BsonDocument("$sum", 1) } }),
            new BsonDocument("$sort", new BsonDocument("count", -1))
        };

        var results = await _col.Aggregate<BsonDocument>(pipeline, cancellationToken: ct).ToListAsync(ct);
        return results.Select(d => new TagBucket(d["_id"].AsString, d["count"].AsInt32)).ToList();
    }

    public async Task<IReadOnlyList<Activity>> GetStaleActivitiesAsync(ObjectId userId, int staleDays, int limit, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow.AddDays(-staleDays);
        var filter = Builders<Activity>.Filter.And(
            Builders<Activity>.Filter.Eq(a => a.UserId, userId),
            Builders<Activity>.Filter.Eq(a => a.IsArchived, false),
            Builders<Activity>.Filter.Or(
                Builders<Activity>.Filter.Lt(a => a.LastViewedAt, cutoff),
                Builders<Activity>.Filter.Eq(a => a.LastViewedAt, null as DateTime?)));
        return await _col.Find(filter)
            .SortBy(a => a.LastViewedAt)
            .Limit(limit)
            .ToListAsync(ct);
    }

    // ---- Helpers ----

    private static FilterDefinition<Activity> BuildBaseFilter(ObjectId userId, ActivityFilter filter)
    {
        var f = Builders<Activity>.Filter.Eq(a => a.UserId, userId);
        if (!filter.IncludeArchived) f &= Builders<Activity>.Filter.Eq(a => a.IsArchived, false);
        if (filter.CategoryId.HasValue) f &= Builders<Activity>.Filter.Eq(a => a.CategoryId, filter.CategoryId.Value);
        if (filter.Tags is { Count: > 0 }) f &= Builders<Activity>.Filter.All(a => a.Tags, filter.Tags);
        if (filter.FavoritesOnly == true) f &= Builders<Activity>.Filter.Eq(a => a.IsFavorite, true);
        if (!string.IsNullOrWhiteSpace(filter.Search))
            f &= Builders<Activity>.Filter.Text(filter.Search, new TextSearchOptions { CaseSensitive = false });
        return f;
    }

    private static FilterDefinition<Activity>? BuildCursorFilter(string? cursor, ActivitySort sort)
    {
        var decoded = CursorHelper.Decode(cursor);
        if (decoded is null) return null;

        var (_, id, dateValue, title) = decoded.Value;
        return sort switch
        {
            ActivitySort.UpdatedDesc when dateValue.HasValue =>
                Builders<Activity>.Filter.Or(
                    Builders<Activity>.Filter.Lt(a => a.UpdatedAt, dateValue.Value),
                    Builders<Activity>.Filter.And(
                        Builders<Activity>.Filter.Eq(a => a.UpdatedAt, dateValue.Value),
                        Builders<Activity>.Filter.Lt(a => a.Id, id))),

            ActivitySort.UpdatedAsc when dateValue.HasValue =>
                Builders<Activity>.Filter.Or(
                    Builders<Activity>.Filter.Gt(a => a.UpdatedAt, dateValue.Value),
                    Builders<Activity>.Filter.And(
                        Builders<Activity>.Filter.Eq(a => a.UpdatedAt, dateValue.Value),
                        Builders<Activity>.Filter.Gt(a => a.Id, id))),

            ActivitySort.CreatedDesc =>
                Builders<Activity>.Filter.Lt(a => a.Id, id),

            ActivitySort.LastViewedDesc when dateValue.HasValue =>
                Builders<Activity>.Filter.Or(
                    Builders<Activity>.Filter.Lt(a => a.LastViewedAt, dateValue.Value),
                    Builders<Activity>.Filter.And(
                        Builders<Activity>.Filter.Eq(a => a.LastViewedAt, dateValue.Value),
                        Builders<Activity>.Filter.Lt(a => a.Id, id))),

            ActivitySort.TitleAsc when title is not null =>
                Builders<Activity>.Filter.Or(
                    Builders<Activity>.Filter.Gt(a => a.Title, title),
                    Builders<Activity>.Filter.And(
                        Builders<Activity>.Filter.Eq(a => a.Title, title),
                        Builders<Activity>.Filter.Gt(a => a.Id, id))),

            _ => null
        };
    }

    private static SortDefinition<Activity> BuildSort(ActivitySort sort) => sort switch
    {
        ActivitySort.UpdatedAsc => Builders<Activity>.Sort.Ascending(a => a.UpdatedAt).Ascending(a => a.Id),
        ActivitySort.TitleAsc => Builders<Activity>.Sort.Ascending(a => a.Title).Ascending(a => a.Id),
        ActivitySort.CreatedDesc => Builders<Activity>.Sort.Descending(a => a.Id),
        ActivitySort.LastViewedDesc => Builders<Activity>.Sort.Descending(a => a.LastViewedAt).Descending(a => a.Id),
        _ => Builders<Activity>.Sort.Descending(a => a.UpdatedAt).Descending(a => a.Id)
    };

    private static string EncodeCursor(ActivitySort sort, Activity a) => sort switch
    {
        ActivitySort.TitleAsc => CursorHelper.Encode(sort, a.Id, null, a.Title),
        ActivitySort.CreatedDesc => CursorHelper.Encode(sort, a.Id, null, null),
        ActivitySort.LastViewedDesc => CursorHelper.Encode(sort, a.Id, a.LastViewedAt, null),
        ActivitySort.UpdatedAsc => CursorHelper.Encode(sort, a.Id, a.UpdatedAt, null),
        _ => CursorHelper.Encode(sort, a.Id, a.UpdatedAt, null)
    };
}
