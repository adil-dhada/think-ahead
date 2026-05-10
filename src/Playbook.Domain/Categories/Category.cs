using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Playbook.Domain.Errors;

namespace Playbook.Domain.Categories;

[BsonIgnoreExtraElements]
public sealed class Category
{
    public ObjectId Id { get; set; }
    public ObjectId UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryColor Color { get; set; }
    public DateTime CreatedAt { get; set; }
    public int SchemaVersion { get; set; } = 1;

    public Category() { }

    public static Category Create(ObjectId userId, string name, CategoryColor color, DateTime nowUtc)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0) throw new DomainValidationException("Category name cannot be empty.");
        return new Category { Id = ObjectId.GenerateNewId(), UserId = userId, Name = trimmed, Color = color, CreatedAt = nowUtc };
    }

    public void Rename(string name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0) throw new DomainValidationException("Category name cannot be empty.");
        Name = trimmed;
    }

    public void ChangeColor(CategoryColor color) => Color = color;
}
