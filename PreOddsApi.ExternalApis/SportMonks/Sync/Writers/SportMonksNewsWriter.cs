using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksNewsWriter : ISportMonksNewsWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksNewsWriter> _logger;

        public SportMonksNewsWriter(
            IConfiguration configuration,
            ILogger<SportMonksNewsWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertNewsAsync(
            IEnumerable<News> newsItems,
            CancellationToken cancellationToken = default)
        {
            var newsList = newsItems
                .Where(news => news != null)
                .GroupBy(news => news.Id > 0
                    ? news.Id
                    : GenerateSyntheticNewsId(
                        ResolveId(news.FixtureId, news.Fixture?.Id),
                        NullIfWhiteSpace(news.Type),
                        NullIfWhiteSpace(news.Title) ?? string.Empty))
                .Select(group => group.Last())
                .ToList();

            if (newsList.Count == 0)
            {
                return;
            }

            await UpsertNewsCollectionAsync(newsList, cancellationToken);
        }

        public async Task UpsertFixtureNewsAsync(
            IEnumerable<Fixture> fixtures,
            CancellationToken cancellationToken = default)
        {
            var newsItems = new List<NewsContext>();

            foreach (var fixture in fixtures.Where(fixture => fixture != null && fixture.Id > 0))
            {
                foreach (var news in fixture.PrematchNews ?? Enumerable.Empty<News>())
                {
                    if (news != null)
                    {
                        newsItems.Add(new NewsContext(news, fixture.Id, "prematch"));
                    }
                }

                foreach (var news in fixture.PostmatchNews ?? Enumerable.Empty<News>())
                {
                    if (news != null)
                    {
                        newsItems.Add(new NewsContext(news, fixture.Id, "postmatch"));
                    }
                }
            }

            if (newsItems.Count == 0)
            {
                return;
            }

            await UpsertNewsCollectionAsync(newsItems, cancellationToken);
        }

        private async Task UpsertNewsCollectionAsync(
            IEnumerable<News> newsItems,
            CancellationToken cancellationToken)
        {
            var contexts = newsItems.Select(news => new NewsContext(news, null, null));
            await UpsertNewsCollectionAsync(contexts, cancellationToken);
        }

        private async Task UpsertNewsCollectionAsync(
            IEnumerable<NewsContext> newsItems,
            CancellationToken cancellationToken)
        {
            var newsList = newsItems
                .Where(context => context.News != null)
                .GroupBy(context => GetNewsId(context))
                .Where(group => group.Key != 0)
                .Select(group => group.Last())
                .ToList();

            if (newsList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var newsCount = 0;
                var lineCount = 0;

                foreach (var context in newsList)
                {
                    var newsId = GetNewsId(context);
                    if (await UpsertNewsAsync(
                            connection,
                            transaction,
                            context,
                            newsId,
                            cancellationToken))
                    {
                        newsCount++;
                    }

                    var sortOrder = 1;
                    foreach (var line in context.News.Lines ?? Enumerable.Empty<NewsItemLine>())
                    {
                        if (await UpsertNewsLineAsync(
                                connection,
                                transaction,
                                newsId,
                                line,
                                sortOrder,
                                cancellationToken))
                        {
                            lineCount++;
                        }

                        sortOrder++;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {NewsCount} news items and {LineCount} news item lines.",
                    newsCount,
                    lineCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task<bool> UpsertNewsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            NewsContext context,
            long newsId,
            CancellationToken cancellationToken)
        {
            var news = context.News;
            var title = NullIfWhiteSpace(news.Title);
            if (newsId == 0 || title == null)
            {
                return false;
            }

            var fixtureId = ResolveId(news.FixtureId, news.Fixture?.Id, context.FallbackFixtureId);
            var leagueId = ResolveId(news.LeagueId, news.League?.Id);
            var type = NullIfWhiteSpace(news.Type) ?? context.FallbackType;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.news (
                    id,
                    fixture_id,
                    league_id,
                    title,
                    type,
                    last_synced_at)
                values (
                    @id,
                    (select id from football.fixtures where id = @fixture_id),
                    (select id from competition.leagues where id = @league_id),
                    @title,
                    @type,
                    now())
                on conflict (id) do update set
                    fixture_id = coalesce(excluded.fixture_id, football.news.fixture_id),
                    league_id = coalesce(excluded.league_id, football.news.league_id),
                    title = excluded.title,
                    type = coalesce(excluded.type, football.news.type),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", newsId));
            command.Parameters.Add(BigIntParameter("fixture_id", NullIfZero(fixtureId)));
            command.Parameters.Add(BigIntParameter("league_id", NullIfZero(leagueId)));
            command.Parameters.Add(Parameter("title", title));
            command.Parameters.Add(Parameter("type", type));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private static async Task<bool> UpsertNewsLineAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long fallbackNewsId,
            NewsItemLine line,
            int sortOrder,
            CancellationToken cancellationToken)
        {
            var newsId = ResolveId(line.NewsitemId, fallbackNewsId);
            var text = NullIfWhiteSpace(line.Text);
            if (newsId == 0 || text == null)
            {
                return false;
            }

            var lineId = line.Id > 0
                ? line.Id
                : GenerateSyntheticNewsLineId(newsId, sortOrder, NullIfWhiteSpace(line.Type), text);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.news_lines (
                    id,
                    news_id,
                    text,
                    type,
                    sort_order,
                    last_synced_at)
                select
                    @id,
                    news.id,
                    @text,
                    @type,
                    @sort_order,
                    now()
                from football.news news
                where news.id = @news_id
                on conflict (id) do update set
                    news_id = excluded.news_id,
                    text = excluded.text,
                    type = coalesce(excluded.type, football.news_lines.type),
                    sort_order = coalesce(excluded.sort_order, football.news_lines.sort_order),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", lineId));
            command.Parameters.Add(Parameter("news_id", newsId));
            command.Parameters.Add(Parameter("text", text));
            command.Parameters.Add(Parameter("type", NullIfWhiteSpace(line.Type)));
            command.Parameters.Add(IntegerParameter("sort_order", sortOrder));

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for news sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static long GetNewsId(NewsContext context)
        {
            if (context.News.Id > 0)
            {
                return context.News.Id;
            }

            var lineNewsId = context.News.Lines?
                .Select(line => line.NewsitemId)
                .FirstOrDefault(newsId => newsId > 0) ?? 0;
            if (lineNewsId > 0)
            {
                return lineNewsId;
            }

            var title = NullIfWhiteSpace(context.News.Title);
            if (title == null)
            {
                return 0;
            }

            return GenerateSyntheticNewsId(
                ResolveId(context.News.FixtureId, context.News.Fixture?.Id, context.FallbackFixtureId),
                NullIfWhiteSpace(context.News.Type) ?? context.FallbackType,
                title);
        }

        private static long GenerateSyntheticNewsId(long fixtureId, string? type, string title)
        {
            var key = string.Join(
                ":",
                fixtureId.ToString(CultureInfo.InvariantCulture),
                type ?? string.Empty,
                title);
            return -Math.Max(1, StableHash(key));
        }

        private static long GenerateSyntheticNewsLineId(
            long newsId,
            int sortOrder,
            string? type,
            string text)
        {
            var key = string.Join(
                ":",
                newsId.ToString(CultureInfo.InvariantCulture),
                sortOrder.ToString(CultureInfo.InvariantCulture),
                type ?? string.Empty,
                text);
            return -Math.Max(1, StableHash(key));
        }

        private static long StableHash(string value)
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;

            var hash = offset;
            foreach (var character in value)
            {
                hash ^= character;
                hash *= prime;
            }

            return (long)(hash % long.MaxValue);
        }

        private static long ResolveId(params long?[] values)
        {
            return values.FirstOrDefault(value => value.GetValueOrDefault() > 0).GetValueOrDefault();
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static NpgsqlParameter BigIntParameter(string name, long? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Bigint)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter IntegerParameter(string name, int? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Integer)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }

        private sealed record NewsContext(News News, long? FallbackFixtureId, string? FallbackType);
    }
}
