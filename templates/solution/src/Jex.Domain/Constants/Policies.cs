namespace Jex.Domain.Constants;

/// <summary>Authorization policy names. Registered in Jex.WebAPI/DependencyInjection.cs.</summary>
public static class Policies
{
    /// <summary>May permanently delete data (purge). Requires <see cref="Roles.Administrator"/>.</summary>
    public const string CanPurge = "CanPurge";
}
