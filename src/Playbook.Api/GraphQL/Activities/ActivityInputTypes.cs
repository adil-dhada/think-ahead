using Playbook.Application.Activities;
using Playbook.Domain.Categories;

namespace Playbook.Api.GraphQL.Activities;

public sealed record ActivityFilterInput(
    string? CategoryId = null,
    List<string>? Tags = null,
    string? Search = null,
    bool? FavoritesOnly = null,
    bool IncludeArchived = false)
{
    public ActivityFilter ToDomain()
    {
        MongoDB.Bson.ObjectId? categoryId = null;
        if (CategoryId is not null && MongoDB.Bson.ObjectId.TryParse(CategoryId, out var parsed))
            categoryId = parsed;
        return new ActivityFilter(categoryId, Tags, Search, FavoritesOnly, IncludeArchived);
    }
}

public sealed record CreateActivityInput(
    string Title,
    string? Description = null,
    string? Notes = null,
    string? CategoryId = null,
    List<string>? Tags = null,
    List<string>? Dos = null,
    List<string>? Donts = null);

public sealed record UpdateActivityInput(
    string Title,
    string? Description = null,
    string? Notes = null,
    string? CategoryId = null,
    List<string>? Tags = null,
    List<string>? Dos = null,
    List<string>? Donts = null);

public sealed record CreateCategoryInput(string Name, CategoryColor Color);
public sealed record UpdateCategoryInput(string? Name = null, CategoryColor? Color = null);
