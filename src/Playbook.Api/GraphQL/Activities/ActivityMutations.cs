using HotChocolate.Authorization;
using HotChocolate.Types;
using MongoDB.Bson;
using Playbook.Application.Activities;
using Playbook.Application.Categories;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Activities;
using Playbook.Domain.Categories;

namespace Playbook.Api.GraphQL.Activities;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class ActivityMutations
{
    private static string Sas(IBlobStore blobs, Domain.Activities.AttachmentRef att, CancellationToken ct) =>
        blobs.GetReadSasUrlAsync(att.BlobPath, TimeSpan.FromMinutes(15), ct).GetAwaiter().GetResult().ToString();

    private static async Task<IReadOnlyDictionary<ObjectId, Category>> SingleCatMap(
        ICategoryRepository categoryRepo, ICurrentUser currentUser, ObjectId? categoryId, CancellationToken ct)
    {
        if (!categoryId.HasValue) return new Dictionary<ObjectId, Category>();
        var cat = await categoryRepo.GetByIdAsync(currentUser.RequireUserId(), categoryId.Value, ct);
        return cat is null ? new Dictionary<ObjectId, Category>() : new Dictionary<ObjectId, Category> { [cat.Id] = cat };
    }

    [Authorize]
    public async Task<ActivityNode> CreateActivityAsync(
        CreateActivityInput input,
        [Service] CreateActivityHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        CancellationToken ct)
    {
        var cmd = new CreateActivityCommand(input.Title, input.Description, input.Notes,
            input.CategoryId, input.Tags, input.Dos, input.Donts);
        var activity = await handler.Handle(cmd, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode> UpdateActivityAsync(
        string id, UpdateActivityInput input,
        [Service] UpdateActivityHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        CancellationToken ct)
    {
        var cmd = new UpdateActivityCommand(id, input.Title, input.Description, input.Notes,
            input.CategoryId, input.Tags, input.Dos, input.Donts);
        var activity = await handler.Handle(cmd, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public Task<bool> DeleteActivityAsync(string id, [Service] DeleteActivityHandler handler, CancellationToken ct) =>
        handler.Handle(id, ct);

    [Authorize]
    public async Task<ActivityNode> ArchiveActivityAsync(
        string id, bool archived,
        [Service] ArchiveActivityHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs, CancellationToken ct)
    {
        var activity = await handler.Handle(id, archived, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode> ToggleFavoriteAsync(
        string id,
        [Service] ToggleFavoriteHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs, CancellationToken ct)
    {
        var activity = await handler.Handle(id, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode> RecordViewAsync(
        string id,
        [Service] RecordViewHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs, CancellationToken ct)
    {
        var activity = await handler.Handle(id, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode> RecordRunAsync(
        string id,
        string? outcomeNote,
        [Service] RecordRunHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs,
        CancellationToken ct)
    {
        var activity = await handler.Handle(new RecordRunCommand(id, outcomeNote), ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode> AttachToActivityAsync(
        string activityId, string blobPath, string fileName, string contentType, long sizeBytes,
        [Service] AttachToActivityHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs, CancellationToken ct)
    {
        var cmd = new AttachToActivityCommand(activityId, blobPath, fileName, contentType, sizeBytes);
        var activity = await handler.Handle(cmd, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<ActivityNode> DetachFromActivityAsync(
        string activityId, string blobPath,
        [Service] DetachFromActivityHandler handler,
        [Service] ICategoryRepository categoryRepo,
        [Service] ICurrentUser currentUser,
        [Service] IBlobStore blobs, CancellationToken ct)
    {
        var activity = await handler.Handle(activityId, blobPath, ct);
        var catMap = await SingleCatMap(categoryRepo, currentUser, activity.CategoryId, ct);
        return ActivityMapper.ToNode(activity, catMap, att => Sas(blobs, att, ct));
    }

    [Authorize]
    public async Task<GraphQL.Auth.UserType> PinActivityAsync(
        string id, [Service] PinActivityHandler handler, CancellationToken ct)
    {
        var user = await handler.Handle(id, ct);
        return Auth.AuthMapper.ToType(user);
    }

    [Authorize]
    public async Task<GraphQL.Auth.UserType> UnpinActivityAsync(
        string id, [Service] UnpinActivityHandler handler, CancellationToken ct)
    {
        var user = await handler.Handle(id, ct);
        return Auth.AuthMapper.ToType(user);
    }

    [Authorize]
    public async Task<CategoryNode> CreateCategoryAsync(
        CreateCategoryInput input,
        [Service] CreateCategoryHandler handler,
        [Service] IActivityRepository activities,
        [Service] ICurrentUser currentUser,
        CancellationToken ct)
    {
        var cat = await handler.Handle(new Application.Categories.CreateCategoryCommand(input.Name, input.Color), ct);
        return ActivityMapper.ToNode(cat, 0);
    }

    [Authorize]
    public async Task<CategoryNode> UpdateCategoryAsync(
        string id, UpdateCategoryInput input,
        [Service] UpdateCategoryHandler handler,
        [Service] IActivityRepository activities,
        [Service] ICurrentUser currentUser,
        CancellationToken ct)
    {
        var cmd = new Application.Categories.UpdateCategoryCommand(id, input.Name, input.Color);
        var cat = await handler.Handle(cmd, ct);
        var counts = await activities.CountByCategoryAsync(currentUser.RequireUserId(), [cat.Id], ct);
        return ActivityMapper.ToNode(cat, counts.TryGetValue(cat.Id, out var n) ? n : 0);
    }

    [Authorize]
    public Task<bool> DeleteCategoryAsync(string id, [Service] DeleteCategoryHandler handler, CancellationToken ct) =>
        handler.Handle(id, ct);
}
