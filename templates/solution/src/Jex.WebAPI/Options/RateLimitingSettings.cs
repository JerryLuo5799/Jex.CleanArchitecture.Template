using System.ComponentModel.DataAnnotations;

namespace Jex.WebAPI.Options;

/// <summary>
/// Bound from the <c>RateLimiting</c> section. The global limiter partitions by user id when
/// authenticated, otherwise by client IP; the <c>auth</c> policy (login) always partitions by IP.
/// </summary>
public sealed class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";
    public const string AuthPolicyName = "auth";

    public bool Enabled { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 100;

    [Range(1, 3600)]
    public int WindowSeconds { get; set; } = 60;

    [Range(1, int.MaxValue)]
    public int AuthPermitLimit { get; set; } = 10;

    [Range(1, 3600)]
    public int AuthWindowSeconds { get; set; } = 60;
}
