using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Activities;
using Playbook.Domain.Errors;
using Playbook.Domain.Users;

namespace Playbook.Application.Activities;

public sealed class DeleteActivityHandler(
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<bool> Handle(string activityId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(activityId);
        var existing = await activities.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Activity", activityId);
        await activities.DeleteAsync(userId, existing.Id, ct);
        return true;
    }
}

public sealed class ArchiveActivityHandler(
    IActivityRepository activities,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Activity> Handle(string activityId, bool archived, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(activityId);
        var activity = await activities.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Activity", activityId);
        activity.Archive(archived, clock.UtcNow);
        await activities.UpdateAsync(activity, ct);
        return activity;
    }
}

public sealed class ToggleFavoriteHandler(
    IActivityRepository activities,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Activity> Handle(string activityId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(activityId);
        var activity = await activities.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Activity", activityId);
        activity.ToggleFavorite(clock.UtcNow);
        await activities.UpdateAsync(activity, ct);
        return activity;
    }
}

public sealed class RecordViewHandler(
    IActivityRepository activities,
    ICurrentUser currentUser,
    IClock clock)
{
    public async Task<Activity> Handle(string activityId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(activityId);
        var activity = await activities.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Activity", activityId);
        activity.RecordView(clock.UtcNow);
        await activities.UpdateAsync(activity, ct);
        return activity;
    }
}

public sealed class PinActivityHandler(
    IUserRepository users,
    IActivityRepository activities,
    ICurrentUser currentUser)
{
    public async Task<User> Handle(string activityId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(activityId);
        _ = await activities.GetByIdAsync(userId, id, ct)
            ?? throw new NotFoundException("Activity", activityId);

        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId.ToString());
        user.Pin(id);
        await users.UpdateAsync(user, ct);
        return user;
    }
}

public sealed class UnpinActivityHandler(
    IUserRepository users,
    ICurrentUser currentUser)
{
    public async Task<User> Handle(string activityId, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var id = ObjectId.Parse(activityId);
        var user = await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId.ToString());
        user.Unpin(id);
        await users.UpdateAsync(user, ct);
        return user;
    }
}
