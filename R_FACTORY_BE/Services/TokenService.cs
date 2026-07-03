using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using R_FACTORY_BE.Auth;
using R_FACTORY_BE.DTOs;
using R_FACTORY_BE.Models.Context;
using R_FACTORY_BE.Models.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace R_FACTORY_BE.Services;

public interface ITokenService
{
    string GenerateAccessToken(User user, List<string> roles);
    string GenerateRefreshToken();
    string HashToken(string rawToken);
    Task<RefreshToken> SaveRefreshTokenAsync(int userId, string rawToken, string? deviceInfo);
    Task<RefreshToken?> ValidateRefreshTokenAsync(string rawToken);
    Task<bool> RotateRefreshTokenAsync(RefreshToken oldToken, string newRawToken, string revokedByIp);
    Task RevokeRefreshTokenAsync(RefreshToken token, string revokedByIp, string reason);
    Task RevokeAllUserTokensAsync(int userId, string reason);
}

public class TokenService : ITokenService
{
    private readonly rtc_factory_oeeContext _db;
    private readonly JwtSettings _jwtSettings;

    public TokenService(rtc_factory_oeeContext db, JwtSettings jwtSettings)
    {
        _db = db;
        _jwtSettings = jwtSettings;
    }

    public string GenerateAccessToken(User user, List<string> roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new("fullName", user.FullName ?? string.Empty),
            new("is_admin", (user.IsAdmin == true).ToString().ToLowerInvariant())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public async Task<RefreshToken> SaveRefreshTokenAsync(int userId, string rawToken, string? deviceInfo)
    {
        var entity = new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(rawToken),
            DeviceInfo = deviceInfo,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<RefreshToken?> ValidateRefreshTokenAsync(string rawToken)
    {
        var hash = HashToken(rawToken);
        var token = await _db.RefreshTokens.Where(t => t.TokenHash == hash).FirstOrDefaultAsync();

        if (token == null) return null;
        if (!token.IsActive) return null;

        return token;
    }

    public async Task<bool> RotateRefreshTokenAsync(RefreshToken oldToken, string newRawToken, string revokedByIp)
    {
        var newHash = HashToken(newRawToken);

        oldToken.RevokedAt = DateTime.UtcNow;
        oldToken.RevokedByIp = revokedByIp;
        oldToken.ReplacedByTokenHash = newHash;
        oldToken.ReasonRevoked = "rotation";

        var newToken = new RefreshToken
        {
            UserId = oldToken.UserId,
            TokenHash = newHash,
            DeviceInfo = oldToken.DeviceInfo,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays)
        };

        _db.RefreshTokens.Add(newToken);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task RevokeRefreshTokenAsync(RefreshToken token, string revokedByIp, string reason)
    {
        token.RevokedAt = DateTime.UtcNow;
        token.RevokedByIp = revokedByIp;
        token.ReasonRevoked = reason;
        await _db.SaveChangesAsync();
    }

    public async Task RevokeAllUserTokensAsync(int userId, string reason)
    {
        var now = DateTime.UtcNow;
        await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAt, now)
                .SetProperty(t => t.ReasonRevoked, reason));
    }
}
