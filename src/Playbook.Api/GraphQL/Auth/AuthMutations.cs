using HotChocolate.Authorization;
using HotChocolate.Types;
using Microsoft.AspNetCore.Http;
using Playbook.Application.Auth;

namespace Playbook.Api.GraphQL.Auth;

[ExtendObjectType(OperationTypeNames.Mutation)]
public sealed class AuthMutations
{
    public async Task<AuthPayloadType> SignupAsync(
        SignupInput input,
        [Service] SignupHandler handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        var cmd = new SignupCommand(input.Email, input.Password, input.DisplayName);
        var payload = await handler.Handle(cmd, ct);
        SetRefreshCookie(httpContextAccessor.HttpContext!, payload.RefreshToken, payload.RefreshTokenExpiresAt);
        return AuthMapper.ToType(payload);
    }

    public async Task<AuthPayloadType> LoginAsync(
        LoginInput input,
        [Service] LoginHandler handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        var cmd = new LoginCommand(input.Email, input.Password);
        var payload = await handler.Handle(cmd, ct);
        SetRefreshCookie(httpContextAccessor.HttpContext!, payload.RefreshToken, payload.RefreshTokenExpiresAt);
        return AuthMapper.ToType(payload);
    }

    public async Task<AuthPayloadType> RefreshTokenAsync(
        [Service] RefreshSessionHandler handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        var rawToken = httpContextAccessor.HttpContext!.Request.Cookies["pb_refresh"];
        var payload = await handler.Handle(rawToken ?? string.Empty, ct);
        SetRefreshCookie(httpContextAccessor.HttpContext, payload.RefreshToken, payload.RefreshTokenExpiresAt);
        return AuthMapper.ToType(payload);
    }

    [Authorize]
    public async Task<bool> LogoutAsync(
        [Service] LogoutHandler handler,
        [Service] IHttpContextAccessor httpContextAccessor,
        CancellationToken ct)
    {
        var rawToken = httpContextAccessor.HttpContext!.Request.Cookies["pb_refresh"];
        await handler.Handle(rawToken, ct);
        httpContextAccessor.HttpContext.Response.Cookies.Delete("pb_refresh");
        return true;
    }

    private static void SetRefreshCookie(HttpContext ctx, string token, DateTime expires) =>
        ctx.Response.Cookies.Append("pb_refresh", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expires,
            Path = "/"
        });
}
