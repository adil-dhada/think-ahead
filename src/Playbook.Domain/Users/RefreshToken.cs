using MongoDB.Bson.Serialization.Attributes;

namespace Playbook.Domain.Users;

[BsonIgnoreExtraElements]
public sealed class RefreshToken
{
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByHash { get; set; }

    public RefreshToken() { }

    public RefreshToken(string tokenHash, DateTime expiresAt)
    {
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
    }

    public bool IsActive(DateTime nowUtc) => RevokedAt is null && nowUtc < ExpiresAt;

    public void Revoke(DateTime nowUtc, string? replacedByHash = null)
    {
        if (RevokedAt is not null) return;
        RevokedAt = nowUtc;
        ReplacedByHash = replacedByHash;
    }
}
