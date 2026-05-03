using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksCompetitionReferenceWriter : ISportMonksCompetitionReferenceWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksCompetitionReferenceWriter> _logger;

        public SportMonksCompetitionReferenceWriter(
            IConfiguration configuration,
            ILogger<SportMonksCompetitionReferenceWriter> logger)
        {
            _connectionString = configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertLeaguesWithHierarchyAsync(
            IEnumerable<League> leagues,
            CancellationToken cancellationToken = default)
        {
            var leagueList = leagues
                .Where(league => league != null && league.Id > 0)
                .GroupBy(league => league.Id)
                .Select(group => group.Last())
                .ToList();

            if (leagueList.Count == 0)
            {
                return;
            }

            ApplyHierarchyFallbacks(leagueList);

            var sports = ExtractSports(leagueList)
                .Where(sport => sport.Id > 0)
                .GroupBy(sport => sport.Id)
                .Select(group => group.OrderBy(sport => IsPlaceholderSport(sport) ? 1 : 0).First())
                .ToList();
            var seasons = ExtractSeasons(leagueList)
                .Where(season => season.Id > 0)
                .GroupBy(season => season.Id)
                .Select(group => group.Last())
                .ToList();
            var stages = ExtractStages(leagueList, seasons)
                .Where(stage => stage.Id > 0)
                .GroupBy(stage => stage.Id)
                .Select(group => group.Last())
                .ToList();
            var rounds = ExtractRounds(stages)
                .Where(round => round.Id > 0)
                .GroupBy(round => round.Id)
                .Select(group => group.Last())
                .ToList();
            var groups = ExtractGroups(seasons, stages)
                .Where(group => group.Id > 0)
                .GroupBy(group => group.Id)
                .Select(group => group.Last())
                .ToList();

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                foreach (var sport in sports)
                {
                    await UpsertSportAsync(connection, transaction, sport, cancellationToken);
                }

                foreach (var league in leagueList)
                {
                    await UpsertLeagueAsync(connection, transaction, league, cancellationToken);
                }

                foreach (var season in seasons)
                {
                    await UpsertSeasonAsync(connection, transaction, season, cancellationToken);
                }

                foreach (var stage in stages)
                {
                    await UpsertStageAsync(connection, transaction, stage, cancellationToken);
                }

                foreach (var group in groups)
                {
                    await UpsertGroupAsync(connection, transaction, group, cancellationToken);
                }

                foreach (var round in rounds)
                {
                    await UpsertRoundAsync(connection, transaction, round, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {SportCount} sports, {LeagueCount} leagues, {SeasonCount} seasons, {StageCount} stages, {GroupCount} groups, and {RoundCount} rounds into competition schema.",
                    sports.Count,
                    leagueList.Count,
                    seasons.Count,
                    stages.Count,
                    groups.Count,
                    rounds.Count);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertSportAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Sport sport,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.sports (
                    id,
                    name,
                    code,
                    last_synced_at)
                values (
                    @id,
                    @name,
                    @code,
                    now())
                on conflict (id) do update set
                    name = case
                        when @is_placeholder then catalog.sports.name
                        else excluded.name
                    end,
                    code = coalesce(excluded.code, catalog.sports.code),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", sport.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(sport.Name, "sport", sport.Id)));
            command.Parameters.Add(Parameter("code", NullIfWhiteSpace(sport.Code)));
            command.Parameters.Add(Parameter("is_placeholder", IsPlaceholderSport(sport)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertLeagueAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            League league,
            CancellationToken cancellationToken)
        {
            if (league.SportId == 0)
            {
                throw new InvalidOperationException(
                    $"SportMonks league {league.Id} cannot be written without a sport_id.");
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.leagues (
                    id,
                    sport_id,
                    country_id,
                    name,
                    active,
                    short_code,
                    image_path,
                    type,
                    sub_type,
                    last_played_at,
                    category,
                    has_jerseys,
                    last_synced_at)
                values (
                    @id,
                    @sport_id,
                    (select id from catalog.countries where id = @country_id),
                    @name,
                    @active,
                    @short_code,
                    @image_path,
                    @type,
                    @sub_type,
                    @last_played_at,
                    @category,
                    @has_jerseys,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    country_id = excluded.country_id,
                    name = excluded.name,
                    active = excluded.active,
                    short_code = excluded.short_code,
                    image_path = excluded.image_path,
                    type = excluded.type,
                    sub_type = excluded.sub_type,
                    last_played_at = excluded.last_played_at,
                    category = excluded.category,
                    has_jerseys = excluded.has_jerseys,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", league.Id));
            command.Parameters.Add(Parameter("sport_id", league.SportId));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(league.CountryId)));
            command.Parameters.Add(Parameter("name", GetRequiredName(league.Name, "league", league.Id)));
            command.Parameters.Add(Parameter("active", league.Active));
            command.Parameters.Add(Parameter("short_code", NullIfWhiteSpace(league.ShortCode)));
            command.Parameters.Add(Parameter("image_path", NullIfWhiteSpace(league.ImagePath)));
            command.Parameters.Add(Parameter("type", NullIfWhiteSpace(league.Type)));
            command.Parameters.Add(Parameter("sub_type", NullIfWhiteSpace(league.SubType)));
            command.Parameters.Add(TimestampTzParameter("last_played_at", NullIfDefault(league.LastPlayedAt)));
            command.Parameters.Add(Parameter("category", NullIfZero(league.Category)));
            command.Parameters.Add(Parameter("has_jerseys", league.HasJerseys));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertSeasonAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Season season,
            CancellationToken cancellationToken)
        {
            EnsureRequiredForeignKey(season.SportId, "sport_id", "season", season.Id);
            EnsureRequiredForeignKey(season.LeagueId, "league_id", "season", season.Id);

            if (season.IsCurrent)
            {
                await ClearCurrentSeasonAsync(connection, transaction, season, cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.seasons (
                    id,
                    sport_id,
                    league_id,
                    tie_breaker_rule_id,
                    name,
                    finished,
                    pending,
                    is_current,
                    starting_at,
                    ending_at,
                    standings_recalculated_at,
                    games_in_current_week,
                    last_synced_at)
                values (
                    @id,
                    @sport_id,
                    @league_id,
                    (select id from competition.standing_rules where id = @tie_breaker_rule_id),
                    @name,
                    @finished,
                    @pending,
                    @is_current,
                    @starting_at,
                    @ending_at,
                    @standings_recalculated_at,
                    @games_in_current_week,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    league_id = excluded.league_id,
                    tie_breaker_rule_id = excluded.tie_breaker_rule_id,
                    name = excluded.name,
                    finished = excluded.finished,
                    pending = excluded.pending,
                    is_current = excluded.is_current,
                    starting_at = excluded.starting_at,
                    ending_at = excluded.ending_at,
                    standings_recalculated_at = excluded.standings_recalculated_at,
                    games_in_current_week = excluded.games_in_current_week,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", season.Id));
            command.Parameters.Add(Parameter("sport_id", season.SportId));
            command.Parameters.Add(Parameter("league_id", season.LeagueId));
            command.Parameters.Add(BigIntParameter("tie_breaker_rule_id", NullIfZero(season.TieBreakerRuleId)));
            command.Parameters.Add(Parameter("name", GetRequiredName(season.Name, "season", season.Id)));
            command.Parameters.Add(Parameter("finished", season.Finished));
            command.Parameters.Add(Parameter("pending", season.Pending));
            command.Parameters.Add(Parameter("is_current", season.IsCurrent));
            command.Parameters.Add(DateParameter("starting_at", season.StartingAt));
            command.Parameters.Add(DateParameter("ending_at", season.EndingAt));
            command.Parameters.Add(TimestampTzParameter("standings_recalculated_at", season.StandingsRecalculatedAt));
            command.Parameters.Add(Parameter("games_in_current_week", season.GamesInCurrentWeek));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertStageAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Stage stage,
            CancellationToken cancellationToken)
        {
            EnsureRequiredForeignKey(stage.SportId, "sport_id", "stage", stage.Id);
            EnsureRequiredForeignKey(stage.LeagueId, "league_id", "stage", stage.Id);
            EnsureRequiredForeignKey(stage.SeasonId, "season_id", "stage", stage.Id);

            if (stage.IsCurrent)
            {
                await ClearCurrentStageAsync(connection, transaction, stage, cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.stages (
                    id,
                    sport_id,
                    league_id,
                    season_id,
                    type_id,
                    name,
                    sort_order,
                    finished,
                    is_current,
                    starting_at,
                    ending_at,
                    games_in_current_week,
                    tie_breaker_rule_id,
                    last_synced_at)
                values (
                    @id,
                    @sport_id,
                    @league_id,
                    @season_id,
                    (select id from catalog.types where id = @type_id),
                    @name,
                    @sort_order,
                    @finished,
                    @is_current,
                    @starting_at,
                    @ending_at,
                    @games_in_current_week,
                    (select id from competition.standing_rules where id = @tie_breaker_rule_id),
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    league_id = excluded.league_id,
                    season_id = excluded.season_id,
                    type_id = excluded.type_id,
                    name = excluded.name,
                    sort_order = excluded.sort_order,
                    finished = excluded.finished,
                    is_current = excluded.is_current,
                    starting_at = excluded.starting_at,
                    ending_at = excluded.ending_at,
                    games_in_current_week = excluded.games_in_current_week,
                    tie_breaker_rule_id = excluded.tie_breaker_rule_id,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", stage.Id));
            command.Parameters.Add(Parameter("sport_id", stage.SportId));
            command.Parameters.Add(Parameter("league_id", stage.LeagueId));
            command.Parameters.Add(Parameter("season_id", stage.SeasonId));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(stage.TypeId)));
            command.Parameters.Add(Parameter("name", GetRequiredName(stage.Name, "stage", stage.Id)));
            command.Parameters.Add(Parameter("sort_order", NullIfZero(stage.SortOrder)));
            command.Parameters.Add(Parameter("finished", stage.Finished));
            command.Parameters.Add(Parameter("is_current", stage.IsCurrent));
            command.Parameters.Add(DateParameter("starting_at", stage.StartingAt));
            command.Parameters.Add(DateParameter("ending_at", stage.EndingAt));
            command.Parameters.Add(Parameter("games_in_current_week", stage.GamesInCurrentWeek));
            command.Parameters.Add(BigIntParameter("tie_breaker_rule_id", NullIfZero(stage.TieBreakerRuleId)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertGroupAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Group group,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.groups (
                    id,
                    sport_id,
                    league_id,
                    season_id,
                    stage_id,
                    name,
                    starting_at,
                    ending_at,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from competition.leagues where id = @league_id),
                    (select id from competition.seasons where id = @season_id),
                    (select id from competition.stages where id = @stage_id),
                    @name,
                    @starting_at,
                    @ending_at,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    league_id = excluded.league_id,
                    season_id = excluded.season_id,
                    stage_id = excluded.stage_id,
                    name = excluded.name,
                    starting_at = excluded.starting_at,
                    ending_at = excluded.ending_at,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", group.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(group.SportId)));
            command.Parameters.Add(BigIntParameter("league_id", NullIfZero(group.LeagueId)));
            command.Parameters.Add(BigIntParameter("season_id", NullIfZero(group.SeasonId)));
            command.Parameters.Add(BigIntParameter("stage_id", NullIfZero(group.StageId)));
            command.Parameters.Add(Parameter("name", GetRequiredName(group.Name, "group", group.Id)));
            command.Parameters.Add(DateParameter("starting_at", group.StartingAt));
            command.Parameters.Add(DateParameter("ending_at", group.EndingAt));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertRoundAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Round round,
            CancellationToken cancellationToken)
        {
            EnsureRequiredForeignKey(round.SportId, "sport_id", "round", round.Id);
            EnsureRequiredForeignKey(round.LeagueId, "league_id", "round", round.Id);
            EnsureRequiredForeignKey(round.SeasonId, "season_id", "round", round.Id);
            EnsureRequiredForeignKey(round.StageId, "stage_id", "round", round.Id);

            if (round.IsCurrent)
            {
                await ClearCurrentRoundAsync(connection, transaction, round, cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.rounds (
                    id,
                    sport_id,
                    league_id,
                    season_id,
                    stage_id,
                    name,
                    finished,
                    is_current,
                    starting_at,
                    ending_at,
                    games_in_current_week,
                    last_synced_at)
                values (
                    @id,
                    @sport_id,
                    @league_id,
                    @season_id,
                    @stage_id,
                    @name,
                    @finished,
                    @is_current,
                    @starting_at,
                    @ending_at,
                    @games_in_current_week,
                    now())
                on conflict (id) do update set
                    sport_id = excluded.sport_id,
                    league_id = excluded.league_id,
                    season_id = excluded.season_id,
                    stage_id = excluded.stage_id,
                    name = excluded.name,
                    finished = excluded.finished,
                    is_current = excluded.is_current,
                    starting_at = excluded.starting_at,
                    ending_at = excluded.ending_at,
                    games_in_current_week = excluded.games_in_current_week,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", round.Id));
            command.Parameters.Add(Parameter("sport_id", round.SportId));
            command.Parameters.Add(Parameter("league_id", round.LeagueId));
            command.Parameters.Add(Parameter("season_id", round.SeasonId));
            command.Parameters.Add(Parameter("stage_id", round.StageId));
            command.Parameters.Add(Parameter("name", GetRequiredName(round.Name, "round", round.Id)));
            command.Parameters.Add(Parameter("finished", round.Finished));
            command.Parameters.Add(Parameter("is_current", round.IsCurrent));
            command.Parameters.Add(DateParameter("starting_at", round.StartingAt));
            command.Parameters.Add(DateParameter("ending_at", round.EndingAt));
            command.Parameters.Add(Parameter("games_in_current_week", round.GamesInCurrentWeek));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task ClearCurrentSeasonAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Season season,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                update competition.seasons
                set is_current = false,
                    updated_at = now()
                where league_id = @league_id
                  and id <> @id
                  and is_current;
                """;
            command.Parameters.Add(Parameter("league_id", season.LeagueId));
            command.Parameters.Add(Parameter("id", season.Id));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task ClearCurrentStageAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Stage stage,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                update competition.stages
                set is_current = false,
                    updated_at = now()
                where season_id = @season_id
                  and id <> @id
                  and is_current;
                """;
            command.Parameters.Add(Parameter("season_id", stage.SeasonId));
            command.Parameters.Add(Parameter("id", stage.Id));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task ClearCurrentRoundAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Round round,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                update competition.rounds
                set is_current = false,
                    updated_at = now()
                where stage_id = @stage_id
                  and id <> @id
                  and is_current;
                """;
            command.Parameters.Add(Parameter("stage_id", round.StageId));
            command.Parameters.Add(Parameter("id", round.Id));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for competition reference sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static IEnumerable<Sport> ExtractSports(IEnumerable<League> leagues)
        {
            foreach (var league in leagues)
            {
                if (league.Sport != null)
                {
                    yield return league.Sport;
                }
                else if (league.SportId > 0)
                {
                    yield return PlaceholderSport(league.SportId);
                }

                foreach (var season in league.Seasons ?? Enumerable.Empty<Season>())
                {
                    if (season.Sport != null)
                    {
                        yield return season.Sport;
                    }
                    else if (season.SportId > 0)
                    {
                        yield return PlaceholderSport(season.SportId);
                    }
                }

                foreach (var stage in league.Stages ?? Enumerable.Empty<Stage>())
                {
                    if (stage.Sport != null)
                    {
                        yield return stage.Sport;
                    }
                    else if (stage.SportId > 0)
                    {
                        yield return PlaceholderSport(stage.SportId);
                    }

                    foreach (var round in stage.Rounds ?? Enumerable.Empty<Round>())
                    {
                        if (round.Sport != null)
                        {
                            yield return round.Sport;
                        }
                        else if (round.SportId > 0)
                        {
                            yield return PlaceholderSport(round.SportId);
                        }
                    }
                }
            }
        }

        private static void ApplyHierarchyFallbacks(IEnumerable<League> leagues)
        {
            foreach (var league in leagues)
            {
                if (league.SportId == 0 && league.Sport?.Id > 0)
                {
                    league.SportId = league.Sport.Id;
                }

                foreach (var season in league.Seasons ?? Enumerable.Empty<Season>())
                {
                    ApplySeasonFallbacks(season, league);
                }

                if (league.CurrentSeason != null)
                {
                    ApplySeasonFallbacks(league.CurrentSeason, league);
                }

                foreach (var stage in league.Stages ?? Enumerable.Empty<Stage>())
                {
                    ApplyStageFallbacks(stage, league);
                }
            }
        }

        private static void ApplySeasonFallbacks(Season season, League league)
        {
            if (season.SportId == 0)
            {
                season.SportId = season.Sport?.Id > 0 ? season.Sport.Id : league.SportId;
            }

            if (season.LeagueId == 0)
            {
                season.LeagueId = season.League?.Id > 0 ? season.League.Id : league.Id;
            }

            foreach (var group in season.Groups ?? Enumerable.Empty<Group>())
            {
                if (group.SportId == 0)
                {
                    group.SportId = season.SportId;
                }

                if (group.LeagueId == 0)
                {
                    group.LeagueId = season.LeagueId;
                }

                if (group.SeasonId == 0)
                {
                    group.SeasonId = season.Id;
                }
            }

            foreach (var stage in season.Stages ?? Enumerable.Empty<Stage>())
            {
                ApplyStageFallbacks(stage, league, season);
            }

            if (season.CurrentStage != null)
            {
                ApplyStageFallbacks(season.CurrentStage, league, season);
            }
        }

        private static void ApplyStageFallbacks(Stage stage, League league, Season? season = null)
        {
            if (stage.SportId == 0)
            {
                stage.SportId = stage.Sport?.Id > 0
                    ? stage.Sport.Id
                    : season?.SportId ?? league.SportId;
            }

            if (stage.LeagueId == 0)
            {
                stage.LeagueId = stage.League?.Id > 0
                    ? stage.League.Id
                    : season?.LeagueId ?? league.Id;
            }

            if (stage.SeasonId == 0)
            {
                stage.SeasonId = stage.Season?.Id > 0
                    ? stage.Season.Id
                    : season?.Id ?? 0;
            }

            foreach (var group in stage.Groups ?? Enumerable.Empty<Group>())
            {
                if (group.SportId == 0)
                {
                    group.SportId = stage.SportId;
                }

                if (group.LeagueId == 0)
                {
                    group.LeagueId = stage.LeagueId;
                }

                if (group.SeasonId == 0)
                {
                    group.SeasonId = stage.SeasonId;
                }

                if (group.StageId == 0)
                {
                    group.StageId = stage.Id;
                }
            }

            foreach (var round in stage.Rounds ?? Enumerable.Empty<Round>())
            {
                ApplyRoundFallbacks(round, stage);
            }

            if (stage.CurrentRound != null)
            {
                ApplyRoundFallbacks(stage.CurrentRound, stage);
            }
        }

        private static void ApplyRoundFallbacks(Round round, Stage stage)
        {
            if (round.SportId == 0)
            {
                round.SportId = round.Sport?.Id > 0 ? round.Sport.Id : stage.SportId;
            }

            if (round.LeagueId == 0)
            {
                round.LeagueId = round.League?.Id > 0 ? round.League.Id : stage.LeagueId;
            }

            if (round.SeasonId == 0)
            {
                round.SeasonId = round.Season?.Id > 0 ? round.Season.Id : stage.SeasonId;
            }

            if (round.StageId == 0)
            {
                round.StageId = round.Stage?.Id > 0 ? round.Stage.Id : stage.Id;
            }
        }

        private static IEnumerable<Season> ExtractSeasons(IEnumerable<League> leagues)
        {
            foreach (var league in leagues)
            {
                foreach (var season in league.Seasons ?? Enumerable.Empty<Season>())
                {
                    yield return season;
                }

                if (league.CurrentSeason != null)
                {
                    yield return league.CurrentSeason;
                }
            }
        }

        private static IEnumerable<Stage> ExtractStages(
            IEnumerable<League> leagues,
            IEnumerable<Season> seasons)
        {
            foreach (var league in leagues)
            {
                foreach (var stage in league.Stages ?? Enumerable.Empty<Stage>())
                {
                    yield return stage;
                }
            }

            foreach (var season in seasons)
            {
                foreach (var stage in season.Stages ?? Enumerable.Empty<Stage>())
                {
                    yield return stage;
                }

                if (season.CurrentStage != null)
                {
                    yield return season.CurrentStage;
                }
            }
        }

        private static IEnumerable<Round> ExtractRounds(IEnumerable<Stage> stages)
        {
            foreach (var stage in stages)
            {
                foreach (var round in stage.Rounds ?? Enumerable.Empty<Round>())
                {
                    yield return round;
                }

                if (stage.CurrentRound != null)
                {
                    yield return stage.CurrentRound;
                }
            }
        }

        private static IEnumerable<Group> ExtractGroups(
            IEnumerable<Season> seasons,
            IEnumerable<Stage> stages)
        {
            foreach (var season in seasons)
            {
                foreach (var group in season.Groups ?? Enumerable.Empty<Group>())
                {
                    yield return group;
                }
            }

            foreach (var stage in stages)
            {
                foreach (var group in stage.Groups ?? Enumerable.Empty<Group>())
                {
                    yield return group;
                }
            }
        }

        private static Sport PlaceholderSport(long id)
        {
            return new Sport
            {
                Id = id,
                Name = $"sport-{id}"
            };
        }

        private static bool IsPlaceholderSport(Sport sport)
        {
            return string.Equals(sport.Name, $"sport-{sport.Id}", StringComparison.Ordinal)
                && string.IsNullOrWhiteSpace(sport.Code);
        }

        private static void EnsureRequiredForeignKey(
            long value,
            string columnName,
            string entityName,
            long entityId)
        {
            if (value == 0)
            {
                throw new InvalidOperationException(
                    $"SportMonks {entityName} {entityId} cannot be written without a {columnName}.");
            }
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

        private static int? NullIfZero(int value)
        {
            return value == 0 ? null : value;
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static long? NullIfZero(long? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static DateTime? NullIfDefault(DateTime value)
        {
            return value == default ? null : value;
        }

        private static DateTime? NormalizeUtc(DateTime? value)
        {
            if (!value.HasValue)
            {
                return null;
            }

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private static NpgsqlParameter DateParameter(string name, DateTime? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Date)
            {
                Value = value.HasValue
                    ? DateOnly.FromDateTime(value.Value)
                    : DBNull.Value
            };
        }

        private static NpgsqlParameter TimestampTzParameter(string name, DateTime? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.TimestampTz)
            {
                Value = NormalizeUtc(value) ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter BigIntParameter(string name, long? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Bigint)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
