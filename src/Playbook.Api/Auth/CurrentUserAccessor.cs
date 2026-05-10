using System.Security.Claims;
using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;

namespace Playbook.Api.Auth;

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly ClaimsPrincipal? _principal = httpContextAccessor.HttpContext?.User;

    public ObjectId? UserId
    {
        get
        {
            var sub = _principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? _principal?.FindFirstValue("sub");
            return sub is not null && ObjectId.TryParse(sub, out var id) ? id : null;
        }
    }

    public string? Email => _principal?.FindFirstValue(ClaimTypes.Email)
                         ?? _principal?.FindFirstValue("email");

    public bool IsAuthenticated => _principal?.Identity?.IsAuthenticated == true;
}
