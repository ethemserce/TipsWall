using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PreOddsApi.DataLayer
{
    public static class PreOddsDatabaseOptions
    {
        private const string DefaultProvider = "postgresql";
        private const string MySqlConnectionName = "PreOddsApiMySqlDb";
        private const string PostgreSqlConnectionName = "PreOddsApiPostgresDb";
        private const string PostgreSqlConnectionEnvironmentKey = "PREODDS_POSTGRES_CONNECTION";
        private const string MySqlConnectionEnvironmentKey = "PREODDS_MYSQL_CONNECTION";
        private const string SensitiveDataLoggingEnvironmentKey = "PREODDS_EF_SENSITIVE_LOGGING";

        public static void Configure(DbContextOptionsBuilder optionsBuilder, IConfiguration configuration, bool useNoTracking = false)
        {
            var provider = GetProvider(configuration);
            var connectionString = GetConnectionString(configuration, provider);

            if (IsPostgreSql(provider))
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

            if (IsSensitiveDataLoggingEnabled(configuration))
            {
                optionsBuilder.EnableSensitiveDataLogging();
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
            var connectionName = IsPostgreSql(provider)
                ? PostgreSqlConnectionName
                : MySqlConnectionName;

            var environmentKey = IsPostgreSql(provider)
                ? PostgreSqlConnectionEnvironmentKey
                : MySqlConnectionEnvironmentKey;

            var connectionString = Environment.GetEnvironmentVariable(environmentKey)
                                   ?? configuration.GetConnectionString(connectionName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{connectionName}' is missing.");
            }

            return connectionString;
        }

        private static bool IsPostgreSql(string provider)
        {
            return provider == "postgresql" || provider == "postgres";
        }

        private static bool IsSensitiveDataLoggingEnabled(IConfiguration configuration)
        {
            var value = Environment.GetEnvironmentVariable(SensitiveDataLoggingEnvironmentKey)
                        ?? configuration["Database:EnableSensitiveDataLogging"]
                        ?? configuration["EnableSensitiveDataLogging"];

            return bool.TryParse(value, out var parsed) && parsed;
        }
    }
}
