using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace Hpn.Api.Infrastructure;

/// <summary>
/// Readiness probe: opens a real connection to Postgres so the integration
/// harness exercises the database, and orchestrators don't route traffic before
/// the datastore is reachable.
/// </summary>
internal sealed class PostgresHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("No Postgres connection string configured.");
        }

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Postgres is not reachable.", ex);
        }
    }
}
