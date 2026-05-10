using MongoDB.Bson;
using MongoDB.Driver;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Users;

namespace Playbook.Infrastructure.Mongo;

public sealed class MongoUserRepository(IMongoDatabase db) : IUserRepository
{
    private readonly IMongoCollection<User> _col = db.GetCollection<User>("users");

    public Task<User?> GetByIdAsync(ObjectId id, CancellationToken ct) =>
        _col.Find(u => u.Id == id).FirstOrDefaultAsync(ct)!;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct) =>
        _col.Find(u => u.Email == email).FirstOrDefaultAsync(ct)!;

    public Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct) =>
        _col.Find(Builders<User>.Filter.ElemMatch(u => u.RefreshTokens,
                rt => rt.TokenHash == tokenHash))
            .FirstOrDefaultAsync(ct)!;

    public Task AddAsync(User user, CancellationToken ct) =>
        _col.InsertOneAsync(user, cancellationToken: ct);

    public Task UpdateAsync(User user, CancellationToken ct) =>
        _col.ReplaceOneAsync(u => u.Id == user.Id, user, cancellationToken: ct);
}
