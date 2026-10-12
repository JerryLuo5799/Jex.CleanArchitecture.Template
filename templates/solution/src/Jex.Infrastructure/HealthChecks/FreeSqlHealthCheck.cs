using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Jex.Infrastructure.HealthChecks;

/// <summary>Readiness check: the database answers a trivial query.</summary>
public sealed class FreeSqlHealthCheck(IFreeSql freeSql) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await freeSql.Ado.ExecuteScalarAsync("SELECT 1", cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Database reachable.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Database unreachable.", ex);
        }
    }
}
