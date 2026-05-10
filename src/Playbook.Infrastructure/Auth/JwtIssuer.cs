using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using Playbook.Application.Common.Abstractions;

namespace Playbook.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "playbook-api";
    public string Audience { get; set; } = "playbook-web";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class JwtIssuer(IOptions<JwtOptions> options) : IJwtIssuer
{
    private readonly JwtOptions _opts = options.Value;

    public IssuedAccessToken IssueAccessToken(ObjectId userId, string email, DateTime nowUtc)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opts.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = nowUtc.AddMinutes(_opts.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _opts.Issuer,
            audience: _opts.Audience,
            claims: [
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            ],
            notBefore: nowUtc,
            expires: expiry,
            signingCredentials: creds);

        return new IssuedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }

    public IssuedRefreshToken IssueRefreshToken(DateTime nowUtc)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var hash = HashRefreshToken(raw);
        var expiry = nowUtc.AddDays(_opts.RefreshTokenDays);
        return new IssuedRefreshToken(raw, hash, expiry);
    }

    public string HashRefreshToken(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
