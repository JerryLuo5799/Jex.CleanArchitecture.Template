using System.Text;
using Jex.Infrastructure.Identity;

namespace Jex.WebAPI;

/// <summary>Web layer registration: authentication, authorization, OpenAPI, error handling.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddWebServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // JwtBearerOptions are derived from the validated JwtOptions so the secret lives in one place.
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

        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();

        return services;
    }
}
