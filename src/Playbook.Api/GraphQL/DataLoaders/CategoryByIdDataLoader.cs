using MongoDB.Bson;
using MongoDB.Driver;
using Playbook.Domain.Categories;

namespace Playbook.Api.GraphQL.DataLoaders;

public sealed class CategoryByIdDataLoader(IMongoDatabase db, IBatchScheduler scheduler, DataLoaderOptions options)
    : BatchDataLoader<ObjectId, Category?>(scheduler, options)
{
    protected override async Task<IReadOnlyDictionary<ObjectId, Category?>> LoadBatchAsync(
        IReadOnlyList<ObjectId> keys, CancellationToken ct)
    {
        var col = db.GetCollection<Category>("categories");
        var items = await col.Find(c => keys.Contains(c.Id)).ToListAsync(ct);
        var dict = items.ToDictionary(c => c.Id, c => (Category?)c);
        foreach (var key in keys.Where(k => !dict.ContainsKey(k))) dict[key] = null;
        return dict;
    }
}
