using FluentValidation;
using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Activities;
using Playbook.Domain.Errors;

namespace Playbook.Application.Activities;

public sealed record CreateActivityCommand(
    string Title,
    string? DescriptionJson,
    string? NotesJson,
    string? CategoryId,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Dos,
    IReadOnlyList<string>? Donts);

public sealed class CreateActivityValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(Activity.MaxTitleLength);

        RuleForEach(x => x.Tags!)
            .MaximumLength(50)
            .When(x => x.Tags is not null);

        RuleForEach(x => x.Dos!)
            .MaximumLength(Activity.MaxItemLength)
            .When(x => x.Dos is not null);

        RuleForEach(x => x.Donts!)
            .MaximumLength(Activity.MaxItemLength)
            .When(x => x.Donts is not null);

        RuleFor(x => x.CategoryId)
            .Must(id => id is null || ObjectId.TryParse(id, out _))
            .WithMessage("CategoryId is not a valid id.");

        RuleFor(x => x.DescriptionJson).Must(BeValidJsonOrNull).WithMessage("DescriptionJson must be valid JSON.");
        RuleFor(x => x.NotesJson).Must(BeValidJsonOrNull).WithMessage("NotesJson must be valid JSON.");
    }

    private static bool BeValidJsonOrNull(string? json) => RichTextJson.IsValid(json);
}

public sealed class CreateActivityHandler(
    IActivityRepository activities,
    ICategoryRepository categories,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<CreateActivityCommand> validator)
{
    public async Task<Activity> Handle(CreateActivityCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var userId = currentUser.RequireUserId();

        ObjectId? categoryId = null;
        if (cmd.CategoryId is not null)
        {
            var parsed = ObjectId.Parse(cmd.CategoryId);
            var category = await categories.GetByIdAsync(userId, parsed, ct)
                ?? throw new NotFoundException("Category", cmd.CategoryId);
            categoryId = category.Id;
        }

        var activity = Activity.Create(
            userId,
            cmd.Title,
            cmd.DescriptionJson,
            cmd.NotesJson,
            categoryId,
            cmd.Tags,
            cmd.Dos,
            cmd.Donts,
            clock.UtcNow);

        await activities.AddAsync(activity, ct);
        return activity;
    }
}
