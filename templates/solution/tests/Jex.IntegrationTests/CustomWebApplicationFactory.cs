using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;

namespace Jex.IntegrationTests;

/// <summary>
/// Boots the API against an isolated SQLite file database (one per factory instance) with a
/// known JWT secret so tests can mint tokens without a login round-trip.
/// </summary>
public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public const string TestJwtSecret = "TestOnly-SuperSecretKey-AtLeast32Chars!";
    public const string TestJwtIssuer = "Jex";
    public const string TestJwtAudience = "Jex";

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"TPLNAMELOWER_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("Database:Provider", "Sqlite");
        builder.UseSetting("Database:AutoSyncStructure", "true");

        builder.UseSetting("Jwt:Secret", TestJwtSecret);
        builder.UseSetting("Jwt:Issuer", TestJwtIssuer);
        builder.UseSetting("Jwt:Audience", TestJwtAudience);
        builder.UseSetting("Jwt:ExpiryMinutes", "60");
    }

    /// <summary>Creates a client whose requests carry a test JWT (optionally with roles).</summary>
    public HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestToken(roles));
        return client;
    }

    public static string GenerateTestToken(params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "1"),
            new(JwtRegisteredClaimNames.Email, "testuser@example.com"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, "1"),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

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
            try
            {
                File.Delete(path);
            }
            catch (IOException)
            {
                // SQLite may still hold the file briefly; the OS temp folder is cleaned eventually.
            }
        }
    }
}
