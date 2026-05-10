using MongoDB.Bson;
using Playbook.Domain.Categories;

namespace Playbook.Application.Common.Abstractions;

public interface ICategoryRepository
{
    Task<Category?> GetByIdAsync(ObjectId userId, ObjectId categoryId, CancellationToken ct);
    Task<IReadOnlyList<Category>> ListAsync(ObjectId userId, CancellationToken ct);
    Task<IReadOnlyList<Category>> GetByIdsAsync(ObjectId userId, IReadOnlyCollection<ObjectId> ids, CancellationToken ct);
    Task<bool> ExistsByNameAsync(ObjectId userId, string name, ObjectId? excludingId, CancellationToken ct);
    Task AddAsync(Category category, CancellationToken ct);
    Task UpdateAsync(Category category, CancellationToken ct);
    Task DeleteAsync(ObjectId userId, ObjectId categoryId, CancellationToken ct);
}
