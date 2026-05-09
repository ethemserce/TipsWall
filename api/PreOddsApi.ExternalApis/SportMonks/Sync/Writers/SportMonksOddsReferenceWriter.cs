using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using PreOddsApi.Entities.SportMonks.Odds.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksOddsReferenceWriter : ISportMonksOddsReferenceWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksOddsReferenceWriter> _logger;

        public SportMonksOddsReferenceWriter(
            IConfiguration configuration,
            ILogger<SportMonksOddsReferenceWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertMarketsAsync(
            IEnumerable<Market> markets,
            CancellationToken cancellationToken = default)
        {
            var marketList = markets
                .Where(market => market != null)
                .GroupBy(market => market.Id)
                .Select(group => group.Last())
                .ToList();

            if (marketList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var market in marketList)
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = """
                        insert into odds.markets (
                            id,
                            legacy_id,
                            name,
                            developer_name,
                            has_winning_calculations,
                            active,
                            last_synced_at)
                        values (
                            @id,
                            @legacy_id,
                            @name,
                            @developer_name,
                            @has_winning_calculations,
                            true,
                            now())
                        on conflict (id) do update set
                            legacy_id = excluded.legacy_id,
                            name = excluded.name,
                            developer_name = excluded.developer_name,
                            has_winning_calculations = excluded.has_winning_calculations,
                            active = true,
                            last_synced_at = now(),
                            updated_at = now();
                        """;
                    command.Parameters.Add(Parameter("id", market.Id));
                    command.Parameters.Add(Parameter("legacy_id", market.LegacyId));
                    command.Parameters.Add(Parameter("name", GetRequiredName(market.Name, "market", market.Id)));
                    command.Parameters.Add(Parameter("developer_name", NullIfWhiteSpace(market.DeveloperName)));
                    command.Parameters.Add(Parameter("has_winning_calculations", market.HasWinningCalculations));

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {MarketCount} SportMonks markets into odds.markets.",
                    marketList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertBookmakersAsync(
            IEnumerable<Bookmaker> bookmakers,
            CancellationToken cancellationToken = default)
        {
            var bookmakerList = bookmakers
                .Where(bookmaker => bookmaker != null)
                .GroupBy(bookmaker => bookmaker.Id)
                .Select(group => group.Last())
                .ToList();

            if (bookmakerList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var bookmaker in bookmakerList)
                {
                    await using var command = connection.CreateCommand();
                    command.Transaction = transaction;
                    command.CommandText = """
                        insert into odds.bookmakers (
                            id,
                            legacy_id,
                            name,
                            active,
                            last_synced_at)
                        values (
                            @id,
                            @legacy_id,
                            @name,
                            true,
                            now())
                        on conflict (id) do update set
                            legacy_id = excluded.legacy_id,
                            name = excluded.name,
                            active = true,
                            last_synced_at = now(),
                            updated_at = now();
                        """;
                    command.Parameters.Add(Parameter("id", bookmaker.Id));
                    command.Parameters.Add(Parameter("legacy_id", bookmaker.LegacyId));
                    command.Parameters.Add(Parameter("name", GetRequiredName(bookmaker.Name, "bookmaker", bookmaker.Id)));

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {BookmakerCount} SportMonks bookmakers into odds.bookmakers.",
                    bookmakerList.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for odds reference sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string GetRequiredName(string? value, string entityName, long id)
        {
            return string.IsNullOrWhiteSpace(value)
                ? $"{entityName}-{id}"
                : value.Trim();
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
