using MongoDB.Bson;
using Playbook.Domain.Users;

namespace Playbook.Application.Common.Abstractions;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(ObjectId id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByRefreshTokenHashAsync(string tokenHash, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}
