using FreeSql;
using Jex.Application.Common.Interfaces;
using Jex.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Jex.Infrastructure;

/// <summary>
/// Infrastructure layer service registration.
/// Configures FreeSQL (Code First) and registers repositories.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=jex.db";

        var dataType = Enum.Parse<DataType>(
            configuration["Database:Provider"] ?? "Sqlite",
            ignoreCase: true);

        var freeSql = new FreeSqlBuilder()
            .UseConnectionString(dataType, connectionString)
            // Code First: automatically sync entity schemas to the database on startup.
            .UseAutoSyncStructure(true)
            .Build();

        services.AddSingleton(freeSql);

        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
