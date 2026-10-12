using System.ComponentModel.DataAnnotations;

namespace Jex.WebAPI.Options;

/// <summary>Bound from the <c>Cors</c> section. Use <c>"*"</c> to allow any origin (development only).</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "Default";

    [Required]
    public string[] AllowedOrigins { get; set; } = [];

    public bool AllowAnyOrigin => AllowedOrigins.Contains("*", StringComparer.Ordinal);
}
