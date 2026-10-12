using Jex.Application;
using Jex.Application.Common;
using Jex.Infrastructure;
using Jex.WebAPI;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Middleware;
using Jex.WebAPI.Options;

// NLog is configured from the "NLog" section of appsettings*.json so the same file drives
// logging, environment overrides and template conditions.
NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
var logger = NLog.LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    if (OpenApiGeneration.IsActive)
    {
        // Build-time document generation starts the app for real; it must not need secrets or a database.
        builder.Configuration.AddInMemoryCollection(OpenApiGeneration.PlaceholderConfiguration);
    }

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, typeof(Program).Assembly);
    builder.Services.AddWebServices();

    var app = builder.Build();

    var (handled, exitCode) = await DatabaseCommands.TryRunAsync(app, args);
    if (handled)
    {
        return exitCode;
    }

    // Middleware order matters: logging observes the final response, the exception handler
    // produces envelopes, authentication runs before rate limiting so limits can be per user.
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseExceptionHandler();
    app.UseStatusCodePages(async statusContext =>
    {
        var http = statusContext.HttpContext;
        if (!ApiResponseWriter.IsApiRequest(http))
        {
            return;
        }

        var (code, message) = http.Response.StatusCode switch
        {
            StatusCodes.Status404NotFound => (ErrorCodes.NotFound, "The requested route does not exist."),
            StatusCodes.Status405MethodNotAllowed => (ErrorCodes.MethodNotAllowed, "The HTTP method is not allowed on this route."),
            StatusCodes.Status415UnsupportedMediaType => (ErrorCodes.Validation, "Unsupported content type. Send 'application/json'."),
            StatusCodes.Status401Unauthorized => (ErrorCodes.Unauthenticated, "Authentication is required."),
            StatusCodes.Status403Forbidden => (ErrorCodes.Forbidden, "You do not have permission to perform this action."),
            >= 400 and < 500 => (ErrorCodes.Validation, "The request could not be processed."),
            _ => (ErrorCodes.Unexpected, "An unexpected error occurred."),
        };

        await ApiResponseWriter.WriteAsync(http, code, message);
    });

    app.UseForwardedHeaders();

    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing")
        && Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    {
        app.UseHttpsRedirection();
    }

    app.UseCors(CorsSettings.PolicyName);
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    app.MapEndpoints();

    app.MapHealthChecks("/health").AllowAnonymous().DisableRateLimiting();
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
        .AllowAnonymous().DisableRateLimiting();
    app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
        .AllowAnonymous().DisableRateLimiting();

    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi().AllowAnonymous().DisableRateLimiting();
        app.MapScalarApiReference(options => options.WithTitle("Jex API")).AllowAnonymous().DisableRateLimiting();
    }

    if (!OpenApiGeneration.IsActive)
    {
        await app.Services.InitialiseDatabaseAsync();
    }

    await app.RunAsync();
    return 0;
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    logger.Error(ex, "Application stopped because of an exception.");
    throw;
}

// Required by WebApplicationFactory in integration tests.
public partial class Program { }
