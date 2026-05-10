using Playbook.Application.Auth;
using Playbook.Domain.Users;

namespace Playbook.Api.GraphQL.Auth;

public sealed record SignupInput(string Email, string Password, string DisplayName);
public sealed record LoginInput(string Email, string Password);

public sealed record AuthPayloadType(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    UserType User);

public sealed record UserType(
    string Id,
    string Email,
    string DisplayName,
    List<string> PinnedActivityIds);

public static class AuthMapper
{
    public static AuthPayloadType ToType(AuthPayload payload) => new(
        payload.AccessToken,
        payload.AccessTokenExpiresAt,
        ToType(payload.User));

    public static UserType ToType(User user) => new(
        user.Id.ToString(),
        user.Email,
        user.DisplayName,
        user.PinnedActivityIds.Select(id => id.ToString()).ToList());
}
