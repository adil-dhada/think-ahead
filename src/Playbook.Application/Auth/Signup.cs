using FluentValidation;
using Playbook.Application.Common.Abstractions;
using Playbook.Domain.Errors;
using Playbook.Domain.Users;

namespace Playbook.Application.Auth;

public sealed record SignupCommand(string Email, string Password, string DisplayName);

public sealed class SignupValidator : AbstractValidator<SignupCommand>
{
    public SignupValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.DisplayName).MaximumLength(80);
    }
}

public sealed class SignupHandler(
    IUserRepository users,
    IPasswordHasher hasher,
    IJwtIssuer jwt,
    IClock clock,
    IValidator<SignupCommand> validator)
{
    public async Task<AuthPayload> Handle(SignupCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var email = cmd.Email.Trim().ToLowerInvariant();

        if (await users.GetByEmailAsync(email, ct) is not null)
        {
            throw new ConflictException("EMAIL_TAKEN", "An account with that email already exists.");
        }

        var user = User.Create(email, hasher.Hash(cmd.Password), cmd.DisplayName, clock.UtcNow);
        var refresh = jwt.IssueRefreshToken(clock.UtcNow);
        user.AddRefreshToken(new RefreshToken(refresh.Hash, refresh.ExpiresAt));
        await users.AddAsync(user, ct);

        var access = jwt.IssueAccessToken(user.Id, user.Email, clock.UtcNow);
        return new AuthPayload(access.Token, access.ExpiresAt, refresh.Raw, refresh.ExpiresAt, user);
    }
}
