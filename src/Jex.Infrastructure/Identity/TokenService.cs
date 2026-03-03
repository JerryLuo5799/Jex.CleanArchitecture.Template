using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Jex.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// Generates signed JWT access tokens for authenticated users.
/// Token settings (secret, issuer, audience, expiry) are read from configuration.
/// </summary>
public sealed class TokenService(IConfiguration configuration)
{
    public string CreateToken(User user)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var secret = jwtSection["Secret"]
            ?? throw new InvalidOperationException("JWT secret is not configured (Jwt:Secret).");
        var issuer   = jwtSection["Issuer"]   ?? "Jex";
        var audience = jwtSection["Audience"] ?? "Jex";
        var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var m) ? m : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
