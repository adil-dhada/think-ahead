using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Errors;
using Playbook.Domain.Users;

namespace Playbook.Application.Auth;

public sealed class RefreshSessionHandler(
    IUserRepository users,
    IJwtIssuer jwt,
    IClock clock)
{
    public async Task<AuthPayload> Handle(string rawRefreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            throw new ForbiddenException("Refresh token missing.");
        }

        var presentedHash = jwt.HashRefreshToken(rawRefreshToken);
        var user = await users.GetByRefreshTokenHashAsync(presentedHash, ct)
            ?? throw new ForbiddenException("Refresh token is invalid.");

        var existing = user.FindRefreshToken(presentedHash);
        if (existing is null)
        {
            throw new ForbiddenException("Refresh token is invalid.");
        }

        var now = clock.UtcNow;
        if (!existing.IsActive(now))
        {
            // Expired or already revoked. If it was revoked-but-unexpired, treat as theft signal
            // and invalidate every refresh token on the account.
            if (existing.RevokedAt is not null && now < existing.ExpiresAt)
            {
                user.RevokeAllRefreshTokens(now);
                await users.UpdateAsync(user, ct);
            }
            throw new ForbiddenException("Refresh token is invalid.");
        }

        var newRefresh = jwt.IssueRefreshToken(now);
        existing.Revoke(now, newRefresh.Hash);
        user.AddRefreshToken(new RefreshToken(newRefresh.Hash, newRefresh.ExpiresAt));
        await users.UpdateAsync(user, ct);

        var access = jwt.IssueAccessToken(user.Id, user.Email, now);
        return new AuthPayload(access.Token, access.ExpiresAt, newRefresh.Raw, newRefresh.ExpiresAt, user);
    }
}
