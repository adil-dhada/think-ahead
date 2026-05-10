using MongoDB.Bson;

namespace Playbook.Application.Common.Abstractions;

public sealed record IssuedAccessToken(string Token, DateTime ExpiresAt);

public sealed record IssuedRefreshToken(string Raw, string Hash, DateTime ExpiresAt);

public interface IJwtIssuer
{
    IssuedAccessToken IssueAccessToken(ObjectId userId, string email, DateTime nowUtc);
    IssuedRefreshToken IssueRefreshToken(DateTime nowUtc);
    string HashRefreshToken(string raw);
}
