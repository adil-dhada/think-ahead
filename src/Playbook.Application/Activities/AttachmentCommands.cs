using FluentValidation;
using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Activities;
using Playbook.Domain.Errors;

namespace Playbook.Application.Activities;

public sealed record AttachToActivityCommand(
    string ActivityId,
    string BlobPath,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed class AttachToActivityValidator : AbstractValidator<AttachToActivityCommand>
{
    public AttachToActivityValidator()
    {
        RuleFor(x => x.ActivityId).Must(id => ObjectId.TryParse(id, out _));
        RuleFor(x => x.BlobPath).NotEmpty();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ContentType).NotEmpty();
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(25 * 1024 * 1024);
    }
}

public sealed class AttachToActivityHandler(
    IActivityRepository activities,
    IBlobStore blobs,
    ICurrentUser currentUser,
    IClock clock,
    IValidator<AttachToActivityCommand> validator)
{
    public async Task<Activity> Handle(AttachToActivityCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var userId = currentUser.RequireUserId();
        var activityId = ObjectId.Parse(cmd.ActivityId);

        var expectedPrefix = $"{userId}/";
        if (!cmd.BlobPath.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            throw new ForbiddenException("Blob path does not belong to the current user.");
        }
        if (!await blobs.ExistsAsync(cmd.BlobPath, ct))
        {
            throw new NotFoundException("Attachment blob", cmd.BlobPath);
        }

        var activity = await activities.GetByIdAsync(userId, activityId, ct)
            ?? throw new NotFoundException("Activity", cmd.ActivityId);

        var attachment = new AttachmentRef(cmd.BlobPath, cmd.FileName, cmd.ContentType, cmd.SizeBytes, clock.UtcNow);
        activity.AttachFile(attachment, clock.UtcNow);
        await activities.UpdateAsync(activity, ct);
        return activity;
    }
}

public sealed class DetachFromActivityHandler(
    IActivityRepository activities,
    IBlobStore blobs,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Activity> Handle(string activityIdRaw, string blobPath, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var activityId = ObjectId.Parse(activityIdRaw);
        var activity = await activities.GetByIdAsync(userId, activityId, ct)
            ?? throw new NotFoundException("Activity", activityIdRaw);

        activity.DetachFile(blobPath, clock.UtcNow);
        await activities.UpdateAsync(activity, ct);
        await blobs.DeleteAsync(blobPath, ct);
        return activity;
    }
}
