using Jex.Application;
using Jex.Application.Common;
using Jex.Application.Common.Exceptions;
using Jex.Infrastructure;
using Jex.WebAPI;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Middleware;
using Jex.WebAPI.Models;

// NLog is configured from the "NLog" section of appsettings*.json so the same file drives
// logging, environment overrides and template conditions.
NLog.LogManager.Setup().LoadConfigurationFromAppSettings();
var logger = NLog.LogManager.GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddWebServices();

    var app = builder.Build();

    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseExceptionHandler(errApp =>
    {
        errApp.Run(async context =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var requestId = RequestIdentity.Get(context);

            var response = exception switch
            {
                ValidationException ve => ApiResponse.Fail(requestId, ve.Code, ve.Message, ve.Errors),
                KnownException ke => ApiResponse.Fail(requestId, ke.Code, ke.Message),
                _ => ApiResponse.Fail(
                    requestId,
                    ErrorCodes.Unexpected,
                    app.Environment.IsDevelopment() ? exception?.Message ?? "Unexpected error." : "An unexpected error occurred."),
            };

            if (exception is not KnownException)
            {
                app.Logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Items[RequestLoggingMiddleware.ApiCodeItemKey] = response.Code;
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsJsonAsync(response);
        });
    });

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    logger.Error(ex, "Application stopped because of an exception.");
    throw;
}

// Required by WebApplicationFactory in integration tests.
public partial class Program { }
