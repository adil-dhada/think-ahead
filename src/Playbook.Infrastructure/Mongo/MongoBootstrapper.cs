using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Playbook.Domain.Activities;
using Playbook.Domain.Categories;
using Playbook.Domain.Users;

namespace Playbook.Infrastructure.Mongo;

public sealed class MongoOptions
{
    public const string Section = "Mongo";
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "playbook";
}

public sealed class MongoBootstrapper(IOptions<MongoOptions> options)
{
    private readonly MongoOptions _opts = options.Value;

    public IMongoDatabase GetDatabase()
    {
        var client = new MongoClient(_opts.ConnectionString);
        return client.GetDatabase(_opts.Database);
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        var db = GetDatabase();
        await EnsureUserIndexesAsync(db, ct);
        await EnsureActivityIndexesAsync(db, ct);
        await EnsureCategoryIndexesAsync(db, ct);
    }

    private static async Task EnsureUserIndexesAsync(IMongoDatabase db, CancellationToken ct)
    {
        var col = db.GetCollection<User>("users");
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "idx_users_email" }),
            new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending("RefreshTokens.TokenHash"),
                new CreateIndexOptions { Name = "idx_users_refreshToken", Sparse = true })
        ], ct);
    }

    private static async Task EnsureActivityIndexesAsync(IMongoDatabase db, CancellationToken ct)
    {
        var col = db.GetCollection<Activity>("activities");
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Ascending(a => a.UserId)
                    .Ascending(a => a.IsArchived)
                    .Descending(a => a.UpdatedAt),
                new CreateIndexOptions { Name = "idx_activities_user_archived_updated" }),
            new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Ascending(a => a.UserId)
                    .Ascending(a => a.CategoryId)
                    .Ascending(a => a.IsArchived)
                    .Descending(a => a.UpdatedAt),
                new CreateIndexOptions { Name = "idx_activities_user_category_archived_updated" }),
            new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Ascending(a => a.UserId)
                    .Ascending(a => a.Tags)
                    .Ascending(a => a.IsArchived),
                new CreateIndexOptions { Name = "idx_activities_user_tags" }),
            new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Ascending(a => a.UserId)
                    .Ascending(a => a.IsFavorite)
                    .Descending(a => a.UpdatedAt),
                new CreateIndexOptions<Activity>
                {
                    Name = "idx_activities_user_favorites",
                    PartialFilterExpression = Builders<Activity>.Filter.Eq(a => a.IsFavorite, true)
                }),
            new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Ascending(a => a.UserId)
                    .Descending(a => a.LastViewedAt),
                new CreateIndexOptions<Activity>
                {
                    Name = "idx_activities_user_lastViewed",
                    PartialFilterExpression = Builders<Activity>.Filter.Exists(a => a.LastViewedAt)
                }),
            new CreateIndexModel<Activity>(
                Builders<Activity>.IndexKeys
                    .Text(a => a.Title)
                    .Text(a => a.DescriptionJson)
                    .Text(a => a.NotesJson),
                new CreateIndexOptions
                {
                    Name = "activity_text",
                    Weights = new BsonDocument { { "Title", 10 }, { "DescriptionJson", 3 }, { "NotesJson", 1 } }
                })
        ], ct);
    }

    private static async Task EnsureCategoryIndexesAsync(IMongoDatabase db, CancellationToken ct)
    {
        var col = db.GetCollection<Category>("categories");
        await col.Indexes.CreateManyAsync([
            new CreateIndexModel<Category>(
                Builders<Category>.IndexKeys.Ascending(c => c.UserId).Ascending(c => c.Name),
                new CreateIndexOptions { Unique = true, Name = "idx_categories_user_name" })
        ], ct);
    }
}
