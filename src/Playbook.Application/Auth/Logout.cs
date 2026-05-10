using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Errors;

namespace Playbook.Application.Auth;

public sealed class LogoutHandler(
    IUserRepository users,
    IJwtIssuer jwt,
    IClock clock)
{
    public async Task<bool> Handle(string? rawRefreshToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawRefreshToken))
        {
            return true;
        }

        var hash = jwt.HashRefreshToken(rawRefreshToken);
        var user = await users.GetByRefreshTokenHashAsync(hash, ct);
        if (user is null) return true;

        var token = user.FindRefreshToken(hash);
        token?.Revoke(clock.UtcNow);
        await users.UpdateAsync(user, ct);
        return true;
    }
}

public sealed class GetMeHandler(
    IUserRepository users,
    ICurrentUser currentUser)
{
    public async Task<Domain.Users.User> Handle(CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        return await users.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId.ToString());
    }
}
