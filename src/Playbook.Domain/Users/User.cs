using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Playbook.Domain.Errors;

namespace Playbook.Domain.Users;

[BsonIgnoreExtraElements]
public sealed class User
{
    public const int MaxPinned = 5;

    public ObjectId Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ObjectId> PinnedActivityIds { get; set; } = new();
    public List<RefreshToken> RefreshTokens { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public int SchemaVersion { get; set; } = 1;

    public User() { }

    public static User Create(string email, string passwordHash, string displayName, DateTime nowUtc)
    {
        var normalizedEmail = (email ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedEmail.Length == 0) throw new DomainValidationException("Email cannot be empty.");
        var name = (displayName ?? string.Empty).Trim();
        if (name.Length == 0) name = normalizedEmail.Split('@')[0];
        return new User { Id = ObjectId.GenerateNewId(), Email = normalizedEmail, PasswordHash = passwordHash, DisplayName = name, CreatedAt = nowUtc };
    }

    public void AddRefreshToken(RefreshToken token, int activeTokenCap = 5)
    {
        RefreshTokens.Add(token);
        if (RefreshTokens.Count > activeTokenCap * 4)
            RefreshTokens = RefreshTokens.OrderByDescending(t => t.ExpiresAt).Take(activeTokenCap * 2).ToList();
    }

    public RefreshToken? FindRefreshToken(string tokenHash) =>
        RefreshTokens.FirstOrDefault(t => t.TokenHash == tokenHash);

    public void RevokeAllRefreshTokens(DateTime nowUtc)
    {
        foreach (var t in RefreshTokens) t.Revoke(nowUtc);
    }

    public void Pin(ObjectId activityId)
    {
        if (PinnedActivityIds.Contains(activityId)) return;
        if (PinnedActivityIds.Count >= MaxPinned) throw new DomainValidationException($"You can pin at most {MaxPinned} activities.");
        PinnedActivityIds.Add(activityId);
    }

    public void Unpin(ObjectId activityId) => PinnedActivityIds.Remove(activityId);

    public void Rename(string displayName)
    {
        var trimmed = (displayName ?? string.Empty).Trim();
        if (trimmed.Length == 0) throw new DomainValidationException("Display name cannot be empty.");
        DisplayName = trimmed;
    }
}
