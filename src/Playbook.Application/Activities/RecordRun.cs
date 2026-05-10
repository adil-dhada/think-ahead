using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Activities;
using Playbook.Domain.Errors;

namespace Playbook.Application.Activities;

public sealed record RecordRunCommand(string ActivityId, string? OutcomeNote);

public sealed class RecordRunHandler(
    IActivityRepository activities,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Activity> Handle(RecordRunCommand cmd, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var activityId = ObjectId.Parse(cmd.ActivityId);
        var activity = await activities.GetByIdAsync(userId, activityId, ct)
            ?? throw new NotFoundException("Activity", cmd.ActivityId);
        activity.RecordRun(cmd.OutcomeNote, clock.UtcNow);
        await activities.UpdateAsync(activity, ct);
        return activity;
    }
}
