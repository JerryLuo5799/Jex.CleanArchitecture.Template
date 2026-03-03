using System.Text;
using Jex.Application;
using Jex.Application.Common.Exceptions;
using Jex.Infrastructure;
using Jex.WebAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ── Layer registrations ─────────────────────────────────────────────────────
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ── JWT Bearer authentication ────────────────────────────────────────────────
var jwtSection = builder.Configuration.GetSection("Jwt");
var secret = jwtSection["Secret"]
    ?? throw new InvalidOperationException("JWT secret is not configured (Jwt:Secret).");

if (secret == "CHANGE-THIS-TO-A-STRONG-SECRET-KEY-AT-LEAST-32-CHARS")
    throw new InvalidOperationException(
        "The default JWT secret placeholder is in use. Set a strong Jwt:Secret in your environment-specific configuration before running in production.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer   = jwtSection["Issuer"]   ?? "Jex",
            ValidAudience = jwtSection["Audience"] ?? "Jex",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        };
    });

builder.Services.AddAuthorization();

// ── ASP.NET Core services ───────────────────────────────────────────────────
builder.Services.AddControllers()
    .ConfigureApplicationPartManager(apm =>
    {
        // Exclude Sannr and its Swashbuckle dependency from MVC type scanning.
        // Sannr.AspNetCore contains source-generator stubs that cause TypeLoadException
        // when scanned by the ControllerFeatureProvider; Swashbuckle 6.4.0 (pulled in
        // by Sannr) is incompatible with the Microsoft.OpenApi 2.x that .NET 10 uses.
        var partsToRemove = apm.ApplicationParts
            .Where(p => p.Name.StartsWith("Sannr") || p.Name.StartsWith("Swashbuckle"))
            .ToList();
        foreach (var part in partsToRemove)
            apm.ApplicationParts.Remove(part);
    });
builder.Services.AddOpenApi();

// ── ProblemDetails for RFC 7807 error responses ─────────────────────────────
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Global exception handler ────────────────────────────────────────────────
app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        var (code, msg) = exception switch
        {
            NotFoundException => (4040, exception.Message),
            ValidationException ve => (4000, ve.Message),
            _ => (5000, "An unexpected error occurred.")
        };

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(context.TraceIdentifier, code, msg);
        await context.Response.WriteAsJsonAsync(response);
    });
});

// ── OpenAPI / Swagger ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Required for WebApplicationFactory in integration tests.
public partial class Program { }
