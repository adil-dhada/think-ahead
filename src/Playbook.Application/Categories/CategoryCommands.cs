using FluentValidation;
using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Categories;
using Playbook.Domain.Errors;

namespace Playbook.Application.Categories;

public sealed record CreateCategoryCommand(string Name, CategoryColor Color);

public sealed class CreateCategoryValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(60);
        RuleFor(x => x.Color).IsInEnum();
    }
}

public sealed class CreateCategoryHandler(
    ICategoryRepository categories,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<CreateCategoryCommand> validator)
{
    public async Task<Category> Handle(CreateCategoryCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var userId = currentUser.RequireUserId();
        if (await categories.ExistsByNameAsync(userId, cmd.Name.Trim(), null, ct))
        {
            throw new ConflictException("CATEGORY_NAME_TAKEN", $"Category '{cmd.Name}' already exists.");
        }
        var category = Category.Create(userId, cmd.Name, cmd.Color, clock.UtcNow);
        await categories.AddAsync(category, ct);
        return category;
    }
}

public sealed record UpdateCategoryCommand(string CategoryId, string? Name, CategoryColor? Color);

public sealed class UpdateCategoryValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryValidator()
    {
        RuleFor(x => x.CategoryId).Must(id => ObjectId.TryParse(id, out _));
        RuleFor(x => x.Name).MaximumLength(60).When(x => x.Name is not null);
        RuleFor(x => x.Color!).IsInEnum().When(x => x.Color.HasValue);
        RuleFor(x => x).Must(x => x.Name is not null || x.Color.HasValue)
            .WithMessage("Provide at least one of Name or Color.");
    }
}

public sealed class UpdateCategoryHandler(
    ICategoryRepository categories,
    ICurrentUser currentUser,
    IValidator<UpdateCategoryCommand> validator)
{
    public async Task<Category> Handle(UpdateCategoryCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(cmd.CategoryId);
        var category = await categories.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Category", cmd.CategoryId);

        if (cmd.Name is not null)
        {
            var trimmed = cmd.Name.Trim();
            if (!string.Equals(trimmed, category.Name, StringComparison.OrdinalIgnoreCase)
                && await categories.ExistsByNameAsync(userId, trimmed, id, ct))
            {
                throw new ConflictException("CATEGORY_NAME_TAKEN", $"Category '{cmd.Name}' already exists.");
            }
            category.Rename(trimmed);
        }
        if (cmd.Color.HasValue)
        {
            category.ChangeColor(cmd.Color.Value);
        }

        await categories.UpdateAsync(category, ct);
        return category;
    }
}

public sealed class DeleteCategoryHandler(
    ICategoryRepository categories,
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<bool> Handle(string categoryId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(categoryId);
        var existing = await categories.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Category", categoryId);
        await activities.DetachCategoryAsync(userId, id, ct);
        await categories.DeleteAsync(userId, existing.Id, ct);
        return true;
    }
}
