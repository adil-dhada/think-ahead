using FluentValidation;
using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Activities;
using Playbook.Domain.Errors;

namespace Playbook.Application.Activities;

public sealed record UpdateActivityCommand(
    string ActivityId,
    string Title,
    string? DescriptionJson,
    string? NotesJson,
    string? CategoryId,
    IReadOnlyList<string>? Tags,
    IReadOnlyList<string>? Dos,
    IReadOnlyList<string>? Donts);

public sealed class UpdateActivityValidator : AbstractValidator<UpdateActivityCommand>
{
    public UpdateActivityValidator()
    {
        RuleFor(x => x.ActivityId).Must(id => ObjectId.TryParse(id, out _));
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Activity.MaxTitleLength);
        RuleFor(x => x.CategoryId)
            .Must(id => id is null || ObjectId.TryParse(id, out _))
            .WithMessage("CategoryId is not a valid id.");
        RuleFor(x => x.DescriptionJson).Must(RichTextJson.IsValid).WithMessage("DescriptionJson must be valid JSON.");
        RuleFor(x => x.NotesJson).Must(RichTextJson.IsValid).WithMessage("NotesJson must be valid JSON.");
        RuleForEach(x => x.Dos!).MaximumLength(Activity.MaxItemLength).When(x => x.Dos is not null);
        RuleForEach(x => x.Donts!).MaximumLength(Activity.MaxItemLength).When(x => x.Donts is not null);
    }
}

public sealed class UpdateActivityHandler(
    IActivityRepository activities,
    ICategoryRepository categories,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<UpdateActivityCommand> validator)
{
    public async Task<Activity> Handle(UpdateActivityCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var userId = currentUser.RequireUserId();
        var activityId = ObjectId.Parse(cmd.ActivityId);

        var activity = await activities.GetByIdAsync(userId, activityId, ct)
            ?? throw new NotFoundException("Activity", cmd.ActivityId);

        ObjectId? categoryId = null;
        if (cmd.CategoryId is not null)
        {
            var parsed = ObjectId.Parse(cmd.CategoryId);
            _ = await categories.GetByIdAsync(userId, parsed, ct)
                ?? throw new NotFoundException("Category", cmd.CategoryId);
            categoryId = parsed;
        }

        activity.Update(
            cmd.Title,
            cmd.DescriptionJson,
            cmd.NotesJson,
            categoryId,
            cmd.Tags,
            cmd.Dos,
            cmd.Donts,
            clock.UtcNow);

        await activities.UpdateAsync(activity, ct);
        return activity;
    }
}
