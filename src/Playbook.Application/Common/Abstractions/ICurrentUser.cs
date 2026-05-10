using MongoDB.Bson;
using Playbook.Domain.Errors;

namespace Playbook.Application.Common.Abstractions;

public interface ICurrentUser
{
    ObjectId? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}

public static class CurrentUserExtensions
{
    public static ObjectId RequireUserId(this ICurrentUser user) =>
        user.UserId ?? throw new ForbiddenException("Authentication required.");
}
