using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Categories;

namespace Playbook.Application.Categories;

public sealed class ListCategoriesHandler(
    ICategoryRepository categories,
    ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<Category>> Handle(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        return await categories.ListAsync(userId, ct);
    }
}
