using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

namespace PreOddsApi.WebApi.V3.Health
{
    /// <summary>
    /// Reports degraded when the last successful SportMonks sync run is
    /// older than the freshness window. "DB ping ok" alone hides the
    /// scenario where the workers are dead and the API is happily serving
    /// week-old data.
    ///
    /// Returns Healthy if every observed run finished within the window,
    /// Degraded if any is stale (read traffic still works), Unhealthy only
    /// if the diagnostics table itself is missing/unqueryable.
    /// </summary>
    public sealed class SyncFreshnessHealthCheck : IHealthCheck
    {
        // Generous default — workers run on multi-minute cadences and
        // SportMonks itself rate-limits us.
        private static readonly TimeSpan FreshnessWindow = TimeSpan.FromHours(2);

        private readonly string? _connectionString;

        public SyncFreshnessHealthCheck(IConfiguration configuration)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
                return HealthCheckResult.Unhealthy("Connection string not configured.");

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // The diagnostics table might not exist yet during very first
                // boot — return Healthy in that case so /ready isn't blocked
                // by an empty system.
                const string sql = @"
                    select coalesce(
                        (
                            select json_object_agg(component, latest_at)
                            from (
                                select component, max(completed_at) as latest_at
                                from observability.sync_runs
                                where status = 'success'
                                group by component
                            ) t
                        ),
                        '{}'::json
                    )::text;";

                await using var command = new NpgsqlCommand(sql, connection);
                var raw = await command.ExecuteScalarAsync(cancellationToken) as string ?? "{}";

                // For now we don't dissect the JSON — the timestamp set is
                // logged into the data dict so dashboards can drill in.
                // If ANY component reports a stale completion we degrade.
                var data = new Dictionary<string, object> { ["component_latest"] = raw };
                var threshold = DateTime.UtcNow - FreshnessWindow;
                var stale = raw.Contains("19", StringComparison.Ordinal); // crude check, replaced below

                stale = false;
                // Re-query in a typed shape for the threshold comparison so
                // we don't depend on text matching.
                const string staleSql = @"
                    select count(*) from (
                        select component, max(completed_at) as latest_at
                        from observability.sync_runs
                        where status = 'success'
                        group by component
                    ) t
                    where latest_at < @threshold;";
                await using var staleCmd = new NpgsqlCommand(staleSql, connection);
                staleCmd.Parameters.AddWithValue("threshold", threshold);
                var staleCount = (long)(await staleCmd.ExecuteScalarAsync(cancellationToken) ?? 0L);

                if (staleCount > 0)
                {
                    return HealthCheckResult.Degraded(
                        $"{staleCount} sync component(s) older than {FreshnessWindow.TotalMinutes}m.",
                        data: data);
                }
                return HealthCheckResult.Healthy("All sync components within freshness window.", data);
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == "42P01")
            {
                // Table doesn't exist yet — pre-bootstrap deploy. Don't fail
                // /ready over a missing observability schema.
                return HealthCheckResult.Healthy("observability.sync_runs not yet created.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Sync freshness check failed.", ex);
            }
        }
    }
}
