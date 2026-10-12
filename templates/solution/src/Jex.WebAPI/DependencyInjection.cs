using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Jex.Application.Common;
using Jex.Infrastructure.Identity;
using Jex.WebAPI.Infrastructure;
using Jex.WebAPI.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.HttpOverrides;

namespace Jex.WebAPI;

/// <summary>
/// Web layer registration: authentication/authorization (envelope-aware), error handling, CORS,
/// rate limiting, OpenAPI and health checks. Everything here is driven by validated options.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        AddAuthentication(services);
        AddErrorHandling(services);
        AddCors(services);
        AddRateLimiting(services);
        AddOpenApi(services);

        services.Configure<ForwardedHeadersOptions>(options =>
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);

        services.AddHealthChecks();

        return services;
    }

    private static void AddAuthentication(IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // JwtBearerOptions derive from the validated JwtOptions so the secret lives in one place.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Value.Issuer,
                    ValidAudience = jwt.Value.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Secret)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        services.AddAuthorization();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, EnvelopeAuthorizationResultHandler>();
    }

    private static void AddErrorHandling(IServiceCollection services)
    {
        services.AddExceptionHandler<ApiExceptionHandler>();
        services.AddProblemDetails();

        // Binding failures (malformed JSON, wrong content type) throw so the exception handler can
        // turn them into a 4000 envelope instead of a bare 400/415.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
    }

    private static void AddCors(IServiceCollection services)
    {
        services.AddOptions<CorsSettings>()
            .BindConfiguration(CorsSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<CorsSettings>>((cors, settings) =>
                cors.AddPolicy(CorsSettings.PolicyName, policy =>
                {
                    if (settings.Value.AllowAnyOrigin)
                    {
                        policy.AllowAnyOrigin();
                    }
                    else if (settings.Value.AllowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(settings.Value.AllowedOrigins).AllowCredentials();
                    }

                    policy.AllowAnyHeader().AllowAnyMethod().WithExposedHeaders(RequestIdentity.HeaderName);
                }));
    }

    private static void AddRateLimiting(IServiceCollection services)
    {
        services.AddOptions<RateLimitingSettings>()
            .BindConfiguration(RateLimitingSettings.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(options => { });
        services.AddOptions<RateLimiterOptions>()
            .Configure<IOptions<RateLimitingSettings>>((limiter, settings) =>
            {
                var s = settings.Value;

                limiter.RejectionStatusCode = StatusCodes.Status200OK;
                limiter.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    {
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    }

                    await ApiResponseWriter.WriteAsync(
                        context.HttpContext, ErrorCodes.TooManyRequests, "Too many requests. Please retry later.", cancellationToken: cancellationToken);
                };

                if (!s.Enabled)
                {
                    limiter.AddPolicy(RateLimitingSettings.AuthPolicyName, _ => RateLimitPartition.GetNoLimiter("disabled"));
                    return;
                }

                limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? ClientIp(context),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = s.PermitLimit,
                            Window = TimeSpan.FromSeconds(s.WindowSeconds),
                            QueueLimit = 0,
                        }));

                limiter.AddPolicy(RateLimitingSettings.AuthPolicyName, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: ClientIp(context),
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = s.AuthPermitLimit,
                            Window = TimeSpan.FromSeconds(s.AuthWindowSeconds),
                            QueueLimit = 0,
                        }));
            });
    }

    private static void AddOpenApi(IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());
    }

    private static string ClientIp(HttpContext context)
        => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
