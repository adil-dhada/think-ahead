using Playbook.Domain.Users;

namespace Playbook.Application.Auth;

public sealed record AuthPayload(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    User User);
