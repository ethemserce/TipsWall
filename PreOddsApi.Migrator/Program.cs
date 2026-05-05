using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace PreOddsApi.Migrator;

internal static class Program
{
    private const int SuccessExitCode = 0;
    private const int UsageErrorExitCode = 2;
    private const int FailureExitCode = 1;

    public static async Task<int> Main(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            Console.Error.WriteLine("PREODDS_POSTGRES_CONNECTION env var is required.");
            return UsageErrorExitCode;
        }

        var migrationsPath = args.Length > 0 && !args[0].StartsWith("--")
            ? args[0]
            : Path.Combine(Directory.GetCurrentDirectory(), "database", "postgres");

        if (!Directory.Exists(migrationsPath))
        {
            Console.Error.WriteLine($"Migrations directory not found: {migrationsPath}");
            return UsageErrorExitCode;
        }

        var dryRun = args.Any(a => string.Equals(a, "--dry-run", StringComparison.OrdinalIgnoreCase));

        Console.WriteLine($"Migrations dir : {migrationsPath}");
        Console.WriteLine($"Dry run        : {dryRun}");

        try
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();

            await EnsureTrackingTableAsync(connection);
            var applied = await LoadAppliedAsync(connection);

            var files = Directory.GetFiles(migrationsPath, "*.sql")
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();

            Console.WriteLine($"Files found    : {files.Count}");
            Console.WriteLine();

            var pending = 0;

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var content = await File.ReadAllTextAsync(file);
                var checksum = ComputeChecksum(content);

                if (applied.TryGetValue(fileName, out var existingChecksum))
                {
                    if (existingChecksum == checksum)
                    {
                        Console.WriteLine($"  [skip] {fileName}");
                        continue;
                    }

                    Console.Error.WriteLine(
                        $"  [FAIL] {fileName} content changed since last apply " +
                        $"(stored {existingChecksum[..8]}, current {checksum[..8]}).");
                    return FailureExitCode;
                }

                pending++;
                if (dryRun)
                {
                    Console.WriteLine($"  [pending] {fileName}");
                    continue;
                }

                Console.Write($"  [apply] {fileName} ... ");
                await ApplyAsync(connection, fileName, content, checksum);
                Console.WriteLine("ok");
            }

            Console.WriteLine();
            Console.WriteLine(dryRun
                ? $"Dry run: {pending} file(s) would be applied."
                : $"Done: {pending} new file(s) applied.");

            return SuccessExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"FAILED: {ex.GetType().Name}: {ex.Message}");
            return FailureExitCode;
        }
    }

    private static async Task EnsureTrackingTableAsync(NpgsqlConnection connection)
    {
        const string sql = """
            create schema if not exists sync;
            create table if not exists sync.schema_migrations (
                file_name text primary key,
                checksum text not null,
                applied_at timestamptz not null default now()
            );
            """;

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<Dictionary<string, string>> LoadAppliedAsync(NpgsqlConnection connection)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        await using var command = new NpgsqlCommand(
            "select file_name, checksum from sync.schema_migrations;", connection);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            dict[reader.GetString(0)] = reader.GetString(1);
        return dict;
    }

    private static async Task ApplyAsync(
        NpgsqlConnection connection,
        string fileName,
        string content,
        string checksum)
    {
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using (var apply = new NpgsqlCommand(content, connection, transaction))
                await apply.ExecuteNonQueryAsync();

            await using var record = new NpgsqlCommand(
                "insert into sync.schema_migrations (file_name, checksum) values (@f, @c);",
                connection, transaction);
            record.Parameters.Add(new NpgsqlParameter("f", fileName));
            record.Parameters.Add(new NpgsqlParameter("c", checksum));
            await record.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static string ComputeChecksum(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
