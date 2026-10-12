namespace Jex.Infrastructure.Persistence;

/// <summary>
/// Bound from the <c>Seed</c> section. Roles are always ensured (idempotent). The administrator is
/// created only when <c>Seed:Admin:Enabled</c> is true, and then both email and password are required
/// (set them through user secrets or environment variables, never in appsettings.json).
/// </summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string[] Roles { get; set; } = [];

    public AdminSeed Admin { get; set; } = new();

    public sealed class AdminSeed
    {
        public bool Enabled { get; set; }

        public string? Email { get; set; }

        public string? Password { get; set; }

        public string FirstName { get; set; } = "System";

        public string LastName { get; set; } = "Administrator";
    }
}

/// <summary>Fails startup when the admin seed is enabled without credentials.</summary>
public sealed class SeedOptionsValidator : IValidateOptions<SeedOptions>
{
    public ValidateOptionsResult Validate(string? name, SeedOptions options)
    {
        if (!options.Admin.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Admin.Email))
        {
            failures.Add("Seed:Admin:Email is required when Seed:Admin:Enabled is true.");
        }

        if (string.IsNullOrWhiteSpace(options.Admin.Password))
        {
            failures.Add("Seed:Admin:Password is required when Seed:Admin:Enabled is true.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
