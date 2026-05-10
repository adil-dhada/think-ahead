using HotChocolate.Authorization;
using HotChocolate.Resolvers;
using MsAuth = Microsoft.AspNetCore.Authorization;

namespace Playbook.Api.Auth;

public sealed class HotChocolateAuthorizationHandler(
    MsAuth.IAuthorizationService authorizationService,
    IHttpContextAccessor httpContextAccessor) : IAuthorizationHandler
{
    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        IMiddlewareContext context,
        AuthorizeDirective directive,
        CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || user.Identity?.IsAuthenticated != true)
            return AuthorizeResult.NotAuthenticated;

        if (!string.IsNullOrEmpty(directive.Policy))
        {
            var result = await authorizationService.AuthorizeAsync(user, null, directive.Policy);
            return result.Succeeded ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed;
        }

        if (directive.Roles is { Count: > 0 })
            return directive.Roles.Any(user.IsInRole) ? AuthorizeResult.Allowed : AuthorizeResult.NotAllowed;

        return AuthorizeResult.Allowed;
    }

    public async ValueTask<AuthorizeResult> AuthorizeAsync(
        HotChocolate.Authorization.AuthorizationContext context,
        IReadOnlyList<AuthorizeDirective> directives,
        CancellationToken ct)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || user.Identity?.IsAuthenticated != true)
            return AuthorizeResult.NotAuthenticated;

        foreach (var directive in directives)
        {
            if (!string.IsNullOrEmpty(directive.Policy))
            {
                var result = await authorizationService.AuthorizeAsync(user, null, directive.Policy);
                if (!result.Succeeded) return AuthorizeResult.NotAllowed;
            }
            else if (directive.Roles is { Count: > 0 })
            {
                if (!directive.Roles.Any(user.IsInRole)) return AuthorizeResult.NotAllowed;
            }
        }

        return AuthorizeResult.Allowed;
    }
}
