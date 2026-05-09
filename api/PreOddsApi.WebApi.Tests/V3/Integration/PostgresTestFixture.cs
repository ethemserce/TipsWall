using System;
using System.IO;
using System.Threading.Tasks;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace PreOddsApi.WebApi.Tests.V3.Integration
{
    /// <summary>
    /// Per-collection Postgres container. The migrations under
    /// `database/postgres/*.sql` are replayed once at fixture init so the
    /// container reflects the same schema as production. Tests share this
    /// fixture (xunit collection) so the container starts once per test run.
    /// </summary>
    public sealed class PostgresTestFixture : IAsyncLifetime
    {
        public PostgreSqlContainer Container { get; }
        public string ConnectionString => Container.GetConnectionString();

        public PostgresTestFixture()
        {
            Container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("preodds_test")
                .WithUsername("preodds")
                .WithPassword("preodds_test")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await Container.StartAsync();
            await ApplyMigrationsAsync();
        }

        public Task DisposeAsync() => Container.DisposeAsync().AsTask();

        private async Task ApplyMigrationsAsync()
        {
            var migrationsDir = LocateMigrationsDir();
            var files = Directory.GetFiles(migrationsDir, "*.sql");
            Array.Sort(files, StringComparer.Ordinal);

            await using var connection = new NpgsqlConnection(ConnectionString);
            await connection.OpenAsync();

            foreach (var path in files)
            {
                var sql = await File.ReadAllTextAsync(path);
                if (string.IsNullOrWhiteSpace(sql)) continue;
                await using var command = new NpgsqlCommand(sql, connection);
                command.CommandTimeout = 60;
                await command.ExecuteNonQueryAsync();
            }
        }

        private static string LocateMigrationsDir()
        {
            // Tests run from bin/Debug/net8.0; walk up to repo root and dive
            // into database/postgres/. Resilient enough for both local + CI.
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "database", "postgres");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            throw new DirectoryNotFoundException(
                "Could not locate database/postgres migrations folder relative to test bin.");
        }
    }

    [CollectionDefinition(Name)]
    public sealed class PostgresCollection : ICollectionFixture<PostgresTestFixture>
    {
        public const string Name = "postgres";
    }
}
