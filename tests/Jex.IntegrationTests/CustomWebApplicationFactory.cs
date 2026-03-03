using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Jex.IntegrationTests;

/// <summary>
/// Configures the web application with an isolated SQLite file database for each test run.
/// Also wires in a known JWT secret so tests can generate tokens without hitting the real auth flow.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
{
    /// <summary>A well-known secret used only in testing.</summary>
    public const string TestJwtSecret  = "TestOnly-SuperSecretKey-AtLeast32Chars!";
    public const string TestJwtIssuer   = "Jex";
    public const string TestJwtAudience = "Jex";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"jex_test_{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // UseSetting overrides values from appsettings.json with the highest priority.
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Database:Provider", "Sqlite");

        // Provide a known JWT secret so the app can validate tokens we generate in tests.
        builder.UseSetting("Jwt:Secret",   TestJwtSecret);
        builder.UseSetting("Jwt:Issuer",   TestJwtIssuer);
        builder.UseSetting("Jwt:Audience", TestJwtAudience);
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-authorised with a test JWT so that
    /// requests to <c>[Authorize]</c>-protected endpoints succeed without a real login round-trip.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken());
        return client;
    }

    /// <summary>Generates a signed JWT that is accepted by the test application.</summary>
    public static string GenerateTestToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "1"),
            new Claim(JwtRegisteredClaimNames.Email, "testuser@example.com"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, "1"),
        };

        var token = new JwtSecurityToken(
            issuer: TestJwtIssuer,
            audience: TestJwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        foreach (var path in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
        {
            if (File.Exists(path))
                try { File.Delete(path); } catch { /* SQLite file may still be held; ignore */ }
        }
    }
}
