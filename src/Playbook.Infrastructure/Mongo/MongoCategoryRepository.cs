using MongoDB.Bson;
using MongoDB.Driver;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Categories;

namespace Playbook.Infrastructure.Mongo;

public sealed class MongoCategoryRepository(IMongoDatabase db) : ICategoryRepository
{
    private readonly IMongoCollection<Category> _col = db.GetCollection<Category>("categories");

    public Task<Category?> GetByIdAsync(ObjectId userId, ObjectId categoryId, CancellationToken ct) =>
        _col.Find(c => c.UserId == userId && c.Id == categoryId).FirstOrDefaultAsync(ct)!;

    public async Task<IReadOnlyList<Category>> ListAsync(ObjectId userId, CancellationToken ct) =>
        await _col.Find(c => c.UserId == userId).SortBy(c => c.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(ObjectId userId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct) =>
        await _col.Find(c => c.UserId == userId && ids.Contains(c.Id)).ToListAsync(ct);

    public async Task<bool> ExistsByNameAsync(ObjectId userId, string name, ObjectId? excludingId, CancellationToken ct)
    {
        var filter = Builders<Category>.Filter.And(
            Builders<Category>.Filter.Eq(c => c.UserId, userId),
            Builders<Category>.Filter.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression($"^{System.Text.RegularExpressions.Regex.Escape(name)}$", "i")));
        if (excludingId.HasValue)
            filter &= Builders<Category>.Filter.Ne(c => c.Id, excludingId.Value);
        return await _col.Find(filter).AnyAsync(ct);
    }

    public Task AddAsync(Category category, CancellationToken ct) =>
        _col.InsertOneAsync(category, cancellationToken: ct);

    public Task UpdateAsync(Category category, CancellationToken ct) =>
        _col.ReplaceOneAsync(c => c.Id == category.Id, category, cancellationToken: ct);

    public Task DeleteAsync(ObjectId userId, ObjectId categoryId, CancellationToken ct) =>
        _col.DeleteOneAsync(c => c.UserId == userId && c.Id == categoryId, ct);
}
