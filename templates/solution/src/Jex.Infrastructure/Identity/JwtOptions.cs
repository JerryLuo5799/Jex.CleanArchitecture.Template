using System.ComponentModel.DataAnnotations;

namespace Jex.Infrastructure.Identity;

/// <summary>
/// JWT settings bound from the <c>Jwt</c> configuration section and validated at startup.
/// The secret is never committed for production: set it via environment variables or user secrets.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 characters.</summary>
    [Required(AllowEmptyStrings = false)]
    [MinLength(32)]
    public string Secret { get; set; } = string.Empty;

    [Required]
    public string Issuer { get; set; } = "Jex";

    [Required]
    public string Audience { get; set; } = "Jex";

    [Range(1, 60 * 24 * 30)]
    public int ExpiryMinutes { get; set; } = 60;
}
