using FluentValidation;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Errors;
using Playbook.Domain.Users;

namespace Playbook.Application.Auth;

public sealed record LoginCommand(string Email, string Password);

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtIssuer jwt,
    IClock clock,
    IValidator<LoginCommand> validator)
{
    public async Task<AuthPayload> Handle(LoginCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var email = cmd.Email.Trim().ToLowerInvariant();

        var user = await users.GetByEmailAsync(email, ct);
        if (user is null || !hasher.Verify(user.PasswordHash, cmd.Password))
        {
            // Same error for both cases — no enumeration.
            throw new ForbiddenException("Invalid credentials.");
        }

        var refresh = jwt.IssueRefreshToken(clock.UtcNow);
        user.AddRefreshToken(new RefreshToken(refresh.Hash, refresh.ExpiresAt));
        await users.UpdateAsync(user, ct);

        var access = jwt.IssueAccessToken(user.Id, user.Email, clock.UtcNow);
        return new AuthPayload(access.Token, access.ExpiresAt, refresh.Raw, refresh.ExpiresAt, user);
    }
}
