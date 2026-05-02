using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PreOddsApi.DataLayer
{
    public static class PreOddsDatabaseOptions
    {
        private const string DefaultProvider = "mysql";
        private const string MySqlConnectionName = "PreOddsApiMySqlDb";
        private const string PostgreSqlConnectionName = "PreOddsApiPostgresDb";

        public static void Configure(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration, bool useNoTracking = false)
        {
            var provider = GetProvider(configuration);
            var connectionString = GetConnectionString(configuration, provider);

            if (provider == "postgresql" || provider == "postgres")
            {
                optionsBuilder.UseNpgsql(connectionString, options =>
                {
                    options.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
            }
            else if (provider == "mysql")
            {
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
            else
            {
                throw new InvalidOperationException($"Unsupported database provider '{provider}'. Use 'postgresql' or 'mysql'.");
            }

            if (useNoTracking)
            {
                optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }

        private static string GetProvider(IConfiguration configuration)
        {
            return (Environment.GetEnvironmentVariable("PREODDS_DB_PROVIDER")
                    ?? configuration["DatabaseProvider"]
                    ?? DefaultProvider)
                .Trim()
                .ToLowerInvariant();
        }

        private static string GetConnectionString(IConfiguration configuration, string provider)
        {
            var connectionName = provider == "postgresql" || provider == "postgres"
                ? PostgreSqlConnectionName
                : MySqlConnectionName;

            var environmentKey = provider == "postgresql" || provider == "postgres"
                ? "PREODDS_POSTGRES_CONNECTION"
                : "PREODDS_MYSQL_CONNECTION";

            var connectionString = Environment.GetEnvironmentVariable(environmentKey)
                                   ?? configuration.GetConnectionString(connectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");
            }

            return connectionString;
        }
    }
}
