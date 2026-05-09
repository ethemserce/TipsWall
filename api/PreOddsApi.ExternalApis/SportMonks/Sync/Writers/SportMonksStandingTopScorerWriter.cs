using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using NpgsqlTypes;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.Standings.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.ExternalApis.SportMonks.Sync.Writers
{
    public sealed class SportMonksStandingTopScorerWriter : ISportMonksStandingTopScorerWriter
    {
        private readonly string? _connectionString;
        private readonly ILogger<SportMonksStandingTopScorerWriter> _logger;

        public SportMonksStandingTopScorerWriter(
            IConfiguration configuration,
            ILogger<SportMonksStandingTopScorerWriter> logger)
        {
            _connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? configuration.GetConnectionString("PreOddsApiPostgresDb");
            _logger = logger;
        }

        public async Task UpsertStandingsAsync(
            IEnumerable<Standing> standings,
            CancellationToken cancellationToken = default)
        {
            var standingList = standings
                .Where(standing => standing != null && standing.Id > 0)
                .GroupBy(standing => standing.Id)
                .Select(group => group.Last())
                .ToList();

            if (standingList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var participantCount = 0;
                var ruleCount = 0;
                var detailCount = 0;
                var formCount = 0;

                foreach (var standing in standingList)
                {
                    if (standing.Participant != null && standing.Participant.Id > 0)
                    {
                        await UpsertParticipantTeamAsync(
                            connection,
                            transaction,
                            standing.Participant,
                            standing.SportId,
                            cancellationToken);
                        participantCount++;
                    }

                    if (standing.StandingRule != null && standing.StandingRule.Id > 0)
                    {
                        if (standing.StandingRule.Type != null && standing.StandingRule.Type.Id > 0)
                        {
                            await UpsertTypeAsync(connection, transaction, standing.StandingRule.Type, cancellationToken);
                        }

                        await UpsertStandingRuleAsync(connection, transaction, standing.StandingRule, cancellationToken);
                        ruleCount++;
                    }

                    await UpsertStandingAsync(connection, transaction, standing, cancellationToken);

                    if (standing.StandingDetail != null)
                    {
                        await DeleteStandingDetailsAsync(connection, transaction, standing.Id, cancellationToken);

                        foreach (var detail in standing.StandingDetail.Where(detail => detail != null && detail.Id > 0))
                        {
                            if (detail.Type != null && detail.Type.Id > 0)
                            {
                                await UpsertTypeAsync(connection, transaction, detail.Type, cancellationToken);
                            }

                            await UpsertStandingDetailAsync(connection, transaction, detail, standing.Id, cancellationToken);
                            detailCount++;
                        }
                    }

                    if (standing.StandingForm != null)
                    {
                        await DeleteStandingFormsAsync(connection, transaction, standing.Id, cancellationToken);

                        foreach (var form in standing.StandingForm.Where(form => form != null && form.Id > 0))
                        {
                            await UpsertStandingFormAsync(connection, transaction, form, standing.Id, cancellationToken);
                            formCount++;
                        }
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {StandingCount} standings, {RuleCount} rules, {DetailCount} details, {FormCount} forms, and {ParticipantCount} participant teams.",
                    standingList.Count,
                    ruleCount,
                    detailCount,
                    formCount,
                    participantCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task UpsertTopScorersAsync(
            IEnumerable<TopScorer> topScorers,
            CancellationToken cancellationToken = default)
        {
            var topScorerList = topScorers
                .Where(topScorer => topScorer != null && topScorer.Id > 0)
                .GroupBy(topScorer => topScorer.Id)
                .Select(group => group.Last())
                .ToList();

            if (topScorerList.Count == 0)
            {
                return;
            }

            await using var connection = await OpenConnectionAsync(cancellationToken);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                var playerCount = 0;
                var participantCount = 0;
                var typeCount = 0;

                foreach (var topScorer in topScorerList)
                {
                    if (topScorer.Type != null && topScorer.Type.Id > 0)
                    {
                        await UpsertTypeAsync(connection, transaction, topScorer.Type, cancellationToken);
                        typeCount++;
                    }

                    if (topScorer.Player != null && topScorer.Player.Id > 0)
                    {
                        await UpsertPlayerAsync(connection, transaction, topScorer.Player, cancellationToken);
                        playerCount++;
                    }

                    if (topScorer.Participant != null && topScorer.Participant.Id > 0)
                    {
                        await UpsertParticipantTeamAsync(
                            connection,
                            transaction,
                            topScorer.Participant,
                            null,
                            cancellationToken);
                        participantCount++;
                    }

                    await UpsertTopScorerAsync(connection, transaction, topScorer, cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Upserted {TopScorerCount} top scorer rows with {PlayerCount} nested players, {ParticipantCount} participant teams, and {TypeCount} types.",
                    topScorerList.Count,
                    playerCount,
                    participantCount,
                    typeCount);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private static async Task UpsertStandingAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Standing standing,
            CancellationToken cancellationToken)
        {
            var participantId = standing.ParticipantId.GetValueOrDefault();
            if (participantId == 0 && standing.Participant?.Id > 0)
            {
                participantId = standing.Participant.Id;
            }

            var sportId = standing.SportId.GetValueOrDefault();
            if (sportId == 0 && standing.Sport?.Id > 0)
            {
                sportId = standing.Sport.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.standings (
                    id,
                    participant_id,
                    sport_id,
                    league_id,
                    season_id,
                    stage_id,
                    group_id,
                    round_id,
                    standing_rule_id,
                    position,
                    result,
                    points,
                    last_synced_at)
                values (
                    @id,
                    (select id from football.teams where id = @participant_id),
                    (select id from catalog.sports where id = @sport_id),
                    (select id from competition.leagues where id = @league_id),
                    (select id from competition.seasons where id = @season_id),
                    (select id from competition.stages where id = @stage_id),
                    (select id from competition.groups where id = @group_id),
                    (select id from competition.rounds where id = @round_id),
                    (select id from competition.standing_rules where id = @standing_rule_id),
                    @position,
                    @result,
                    @points,
                    now())
                on conflict (id) do update set
                    participant_id = coalesce(excluded.participant_id, competition.standings.participant_id),
                    sport_id = coalesce(excluded.sport_id, competition.standings.sport_id),
                    league_id = coalesce(excluded.league_id, competition.standings.league_id),
                    season_id = coalesce(excluded.season_id, competition.standings.season_id),
                    stage_id = coalesce(excluded.stage_id, competition.standings.stage_id),
                    group_id = coalesce(excluded.group_id, competition.standings.group_id),
                    round_id = coalesce(excluded.round_id, competition.standings.round_id),
                    standing_rule_id = coalesce(excluded.standing_rule_id, competition.standings.standing_rule_id),
                    position = coalesce(excluded.position, competition.standings.position),
                    result = coalesce(excluded.result, competition.standings.result),
                    points = coalesce(excluded.points, competition.standings.points),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", standing.Id));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(participantId)));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("league_id", NullIfZero(standing.LeagueId)));
            command.Parameters.Add(BigIntParameter("season_id", NullIfZero(standing.SeasonId)));
            command.Parameters.Add(BigIntParameter("stage_id", NullIfZero(standing.StageId)));
            command.Parameters.Add(BigIntParameter("group_id", NullIfZero(standing.GroupId)));
            command.Parameters.Add(BigIntParameter("round_id", NullIfZero(standing.RoundId)));
            command.Parameters.Add(BigIntParameter("standing_rule_id", NullIfZero(standing.StandingRuleId)));
            command.Parameters.Add(IntegerParameter("position", standing.Position));
            command.Parameters.Add(TextParameter("result", NullIfWhiteSpace(standing.Result)));
            command.Parameters.Add(IntegerParameter("points", standing.Points));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertStandingRuleAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            StandingRule rule,
            CancellationToken cancellationToken)
        {
            var typeId = rule.TypeId;
            if (typeId == 0 && rule.Type?.Id > 0)
            {
                typeId = rule.Type.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.standing_rules (
                    id,
                    model_type,
                    model_id,
                    type_id,
                    position,
                    last_synced_at)
                values (
                    @id,
                    @model_type,
                    @model_id,
                    (select id from catalog.types where id = @type_id),
                    @position,
                    now())
                on conflict (id) do update set
                    model_type = coalesce(excluded.model_type, competition.standing_rules.model_type),
                    model_id = coalesce(excluded.model_id, competition.standing_rules.model_id),
                    type_id = coalesce(excluded.type_id, competition.standing_rules.type_id),
                    position = coalesce(excluded.position, competition.standing_rules.position),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", rule.Id));
            command.Parameters.Add(TextParameter("model_type", NullIfWhiteSpace(rule.ModelType)));
            command.Parameters.Add(BigIntParameter("model_id", NullIfZero(rule.ModelId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(typeId)));
            command.Parameters.Add(IntegerParameter("position", NullIfZero(rule.Position)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertStandingDetailAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            StandingDetail detail,
            long fallbackStandingId,
            CancellationToken cancellationToken)
        {
            var standingId = detail.StandingId.GetValueOrDefault();
            if (standingId == 0)
            {
                standingId = fallbackStandingId;
            }

            var typeId = detail.TypeId.GetValueOrDefault();
            if (typeId == 0 && detail.Type?.Id > 0)
            {
                typeId = detail.Type.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.standing_details (
                    id,
                    standing_type,
                    standing_id,
                    type_id,
                    value,
                    last_synced_at)
                select
                    @id,
                    @standing_type,
                    @standing_id,
                    (select id from catalog.types where id = @type_id),
                    @value,
                    now()
                where exists (select 1 from competition.standings where id = @standing_id)
                on conflict (id) do update set
                    standing_type = coalesce(excluded.standing_type, competition.standing_details.standing_type),
                    standing_id = excluded.standing_id,
                    type_id = coalesce(excluded.type_id, competition.standing_details.type_id),
                    value = excluded.value,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", detail.Id));
            command.Parameters.Add(TextParameter("standing_type", NullIfWhiteSpace(detail.StandingType)));
            command.Parameters.Add(Parameter("standing_id", standingId));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(typeId)));
            command.Parameters.Add(IntegerParameter("value", detail.Value));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertStandingFormAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            StandingForm form,
            long fallbackStandingId,
            CancellationToken cancellationToken)
        {
            var standingId = form.StandingId == 0 ? fallbackStandingId : form.StandingId;

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.standing_forms (
                    id,
                    standing_type,
                    standing_id,
                    fixture_id,
                    form,
                    sort_order,
                    last_synced_at)
                select
                    @id,
                    @standing_type,
                    @standing_id,
                    (select id from football.fixtures where id = @fixture_id),
                    @form,
                    @sort_order,
                    now()
                where exists (select 1 from competition.standings where id = @standing_id)
                on conflict (id) do update set
                    standing_type = coalesce(excluded.standing_type, competition.standing_forms.standing_type),
                    standing_id = excluded.standing_id,
                    fixture_id = coalesce(excluded.fixture_id, competition.standing_forms.fixture_id),
                    form = coalesce(excluded.form, competition.standing_forms.form),
                    sort_order = excluded.sort_order,
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", form.Id));
            command.Parameters.Add(TextParameter("standing_type", NullIfWhiteSpace(form.StandingType)));
            command.Parameters.Add(Parameter("standing_id", standingId));
            command.Parameters.Add(BigIntParameter("fixture_id", NullIfZero(form.FixtureId)));
            command.Parameters.Add(TextParameter("form", NullIfWhiteSpace(form.Form)));
            command.Parameters.Add(Parameter("sort_order", form.SortOrder));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertTopScorerAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            TopScorer topScorer,
            CancellationToken cancellationToken)
        {
            var seasonId = topScorer.SeasonId.GetValueOrDefault();
            if (seasonId == 0 && topScorer.Season?.Id > 0)
            {
                seasonId = topScorer.Season.Id;
            }

            var stageId = topScorer.StageId.GetValueOrDefault();
            if (stageId == 0 && topScorer.Stage?.Id > 0)
            {
                stageId = topScorer.Stage.Id;
            }

            var playerId = topScorer.PlayerId.GetValueOrDefault();
            if (playerId == 0 && topScorer.Player?.Id > 0)
            {
                playerId = topScorer.Player.Id;
            }

            var participantId = topScorer.ParticipantId.GetValueOrDefault();
            if (participantId == 0 && topScorer.Participant?.Id > 0)
            {
                participantId = topScorer.Participant.Id;
            }

            var typeId = topScorer.TypeId.GetValueOrDefault();
            if (typeId == 0 && topScorer.Type?.Id > 0)
            {
                typeId = topScorer.Type.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into competition.top_scorers (
                    id,
                    season_id,
                    stage_id,
                    player_id,
                    type_id,
                    position,
                    total,
                    participant_type,
                    participant_id,
                    last_synced_at)
                values (
                    @id,
                    (select id from competition.seasons where id = @season_id),
                    (select id from competition.stages where id = @stage_id),
                    (select id from football.players where id = @player_id),
                    (select id from catalog.types where id = @type_id),
                    @position,
                    @total,
                    @participant_type,
                    (select id from football.teams where id = @participant_id),
                    now())
                on conflict (id) do update set
                    season_id = coalesce(excluded.season_id, competition.top_scorers.season_id),
                    stage_id = coalesce(excluded.stage_id, competition.top_scorers.stage_id),
                    player_id = coalesce(excluded.player_id, competition.top_scorers.player_id),
                    type_id = coalesce(excluded.type_id, competition.top_scorers.type_id),
                    position = excluded.position,
                    total = excluded.total,
                    participant_type = coalesce(excluded.participant_type, competition.top_scorers.participant_type),
                    participant_id = coalesce(excluded.participant_id, competition.top_scorers.participant_id),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", topScorer.Id));
            command.Parameters.Add(BigIntParameter("season_id", NullIfZero(seasonId)));
            command.Parameters.Add(BigIntParameter("stage_id", NullIfZero(stageId)));
            command.Parameters.Add(BigIntParameter("player_id", NullIfZero(playerId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(typeId)));
            command.Parameters.Add(Parameter("position", topScorer.Position));
            command.Parameters.Add(Parameter("total", topScorer.Total));
            command.Parameters.Add(TextParameter("participant_type", NullIfWhiteSpace(topScorer.ParticipantType)));
            command.Parameters.Add(BigIntParameter("participant_id", NullIfZero(participantId)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertParticipantTeamAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Participant participant,
            long? fallbackSportId,
            CancellationToken cancellationToken)
        {
            var sportId = participant.SportId.GetValueOrDefault();
            if (sportId == 0)
            {
                sportId = fallbackSportId.GetValueOrDefault();
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.teams (
                    id,
                    sport_id,
                    country_id,
                    venue_id,
                    gender,
                    name,
                    short_code,
                    image_path,
                    founded,
                    type,
                    placeholder,
                    last_played_at,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from catalog.countries where id = @country_id),
                    (select id from football.venues where id = @venue_id),
                    @gender,
                    @name,
                    @short_code,
                    @image_path,
                    @founded,
                    @type,
                    @placeholder,
                    @last_played_at,
                    now())
                on conflict (id) do update set
                    sport_id = coalesce(excluded.sport_id, football.teams.sport_id),
                    country_id = coalesce(excluded.country_id, football.teams.country_id),
                    venue_id = coalesce(excluded.venue_id, football.teams.venue_id),
                    gender = coalesce(excluded.gender, football.teams.gender),
                    name = excluded.name,
                    short_code = coalesce(excluded.short_code, football.teams.short_code),
                    image_path = coalesce(excluded.image_path, football.teams.image_path),
                    founded = coalesce(excluded.founded, football.teams.founded),
                    type = coalesce(excluded.type, football.teams.type),
                    placeholder = coalesce(excluded.placeholder, football.teams.placeholder),
                    last_played_at = coalesce(excluded.last_played_at, football.teams.last_played_at),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", participant.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(participant.CountryId)));
            command.Parameters.Add(BigIntParameter("venue_id", NullIfZero(participant.VenueId)));
            command.Parameters.Add(TextParameter("gender", participant.Gender?.ToString()));
            command.Parameters.Add(Parameter("name", GetRequiredName(participant.Name, "team", participant.Id)));
            command.Parameters.Add(TextParameter("short_code", NullIfWhiteSpace(participant.ShortCode)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(participant.ImagePath)));
            command.Parameters.Add(IntegerParameter("founded", ToInt32OrNull(participant.Founded)));
            command.Parameters.Add(TextParameter("type", NullIfWhiteSpace(participant.Type)));
            command.Parameters.Add(BooleanParameter("placeholder", participant.Placeholder));
            command.Parameters.Add(TimestampTzParameter("last_played_at", participant.LastPlayedAt));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertPlayerAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Player player,
            CancellationToken cancellationToken)
        {
            var sportId = player.SportId.GetValueOrDefault();
            if (sportId == 0 && player.Sport?.Id > 0)
            {
                sportId = player.Sport.Id;
            }

            var countryId = player.CountryId.GetValueOrDefault();
            if (countryId == 0 && player.Country?.Id > 0)
            {
                countryId = player.Country.Id;
            }

            var nationalityId = player.NationalityId.GetValueOrDefault();
            if (nationalityId == 0 && player.Nationality?.Id > 0)
            {
                nationalityId = player.Nationality.Id;
            }

            var cityId = player.CityId.GetValueOrDefault();
            if (cityId == 0 && player.City?.Id > 0)
            {
                cityId = player.City.Id;
            }

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into football.players (
                    id,
                    sport_id,
                    country_id,
                    nationality_id,
                    city_id,
                    position_id,
                    detailed_position_id,
                    type_id,
                    common_name,
                    first_name,
                    last_name,
                    name,
                    display_name,
                    gender,
                    image_path,
                    height,
                    weight,
                    date_of_birth,
                    last_synced_at)
                values (
                    @id,
                    (select id from catalog.sports where id = @sport_id),
                    (select id from catalog.countries where id = @country_id),
                    (select id from catalog.countries where id = @nationality_id),
                    (select id from catalog.cities where id = @city_id),
                    (select id from catalog.types where id = @position_id),
                    (select id from catalog.types where id = @detailed_position_id),
                    (select id from catalog.types where id = @type_id),
                    @common_name,
                    @first_name,
                    @last_name,
                    @name,
                    @display_name,
                    @gender,
                    @image_path,
                    @height,
                    @weight,
                    @date_of_birth,
                    now())
                on conflict (id) do update set
                    sport_id = coalesce(excluded.sport_id, football.players.sport_id),
                    country_id = coalesce(excluded.country_id, football.players.country_id),
                    nationality_id = coalesce(excluded.nationality_id, football.players.nationality_id),
                    city_id = coalesce(excluded.city_id, football.players.city_id),
                    position_id = coalesce(excluded.position_id, football.players.position_id),
                    detailed_position_id = coalesce(excluded.detailed_position_id, football.players.detailed_position_id),
                    type_id = coalesce(excluded.type_id, football.players.type_id),
                    common_name = coalesce(excluded.common_name, football.players.common_name),
                    first_name = coalesce(excluded.first_name, football.players.first_name),
                    last_name = coalesce(excluded.last_name, football.players.last_name),
                    name = excluded.name,
                    display_name = coalesce(excluded.display_name, football.players.display_name),
                    gender = coalesce(excluded.gender, football.players.gender),
                    image_path = coalesce(excluded.image_path, football.players.image_path),
                    height = coalesce(excluded.height, football.players.height),
                    weight = coalesce(excluded.weight, football.players.weight),
                    date_of_birth = coalesce(excluded.date_of_birth, football.players.date_of_birth),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", player.Id));
            command.Parameters.Add(BigIntParameter("sport_id", NullIfZero(sportId)));
            command.Parameters.Add(BigIntParameter("country_id", NullIfZero(countryId)));
            command.Parameters.Add(BigIntParameter("nationality_id", NullIfZero(nationalityId)));
            command.Parameters.Add(BigIntParameter("city_id", NullIfZero(cityId)));
            command.Parameters.Add(BigIntParameter("position_id", NullIfZero(player.PositionId)));
            command.Parameters.Add(BigIntParameter("detailed_position_id", NullIfZero(player.DetailedPositionId)));
            command.Parameters.Add(BigIntParameter("type_id", NullIfZero(player.TypeId)));
            command.Parameters.Add(TextParameter("common_name", NullIfWhiteSpace(player.CommonName)));
            command.Parameters.Add(TextParameter("first_name", NullIfWhiteSpace(player.FirstName)));
            command.Parameters.Add(TextParameter("last_name", NullIfWhiteSpace(player.LastName)));
            command.Parameters.Add(Parameter("name", GetRequiredPlayerName(player)));
            command.Parameters.Add(TextParameter("display_name", NullIfWhiteSpace(player.DisplayName)));
            command.Parameters.Add(TextParameter("gender", NullIfWhiteSpace(player.Gender)));
            command.Parameters.Add(TextParameter("image_path", NullIfWhiteSpace(player.ImagePath)));
            command.Parameters.Add(IntegerParameter("height", player.Height));
            command.Parameters.Add(IntegerParameter("weight", player.Weight));
            command.Parameters.Add(DateParameter("date_of_birth", player.DateOfBirth));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task UpsertTypeAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            Types type,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                insert into catalog.types (
                    id,
                    name,
                    code,
                    developer_name,
                    model_type,
                    stat_group,
                    last_synced_at)
                values (
                    @id,
                    @name,
                    @code,
                    @developer_name,
                    @model_type,
                    @stat_group,
                    now())
                on conflict (id) do update set
                    name = excluded.name,
                    code = coalesce(excluded.code, catalog.types.code),
                    developer_name = coalesce(excluded.developer_name, catalog.types.developer_name),
                    model_type = coalesce(excluded.model_type, catalog.types.model_type),
                    stat_group = coalesce(excluded.stat_group, catalog.types.stat_group),
                    last_synced_at = now(),
                    updated_at = now();
                """;
            command.Parameters.Add(Parameter("id", type.Id));
            command.Parameters.Add(Parameter("name", GetRequiredName(type.Name, "type", type.Id)));
            command.Parameters.Add(TextParameter("code", NullIfWhiteSpace(type.Code)));
            command.Parameters.Add(TextParameter("developer_name", NullIfWhiteSpace(type.DeveloperName)));
            command.Parameters.Add(TextParameter("model_type", NullIfWhiteSpace(type.ModelType)));
            command.Parameters.Add(TextParameter("stat_group", NullIfWhiteSpace(type.StatGroup)));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task DeleteStandingDetailsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long standingId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "delete from competition.standing_details where standing_id = @standing_id;";
            command.Parameters.Add(Parameter("standing_id", standingId));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private static async Task DeleteStandingFormsAsync(
            NpgsqlConnection connection,
            NpgsqlTransaction transaction,
            long standingId,
            CancellationToken cancellationToken)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "delete from competition.standing_forms where standing_id = @standing_id;";
            command.Parameters.Add(Parameter("standing_id", standingId));

            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException(
                    "PostgreSQL connection string 'PreOddsApiPostgresDb' is required for standings/top scorers sync.");
            }

            var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        private static string GetRequiredPlayerName(Player player)
        {
            return FirstNonEmpty(
                player.Name,
                player.DisplayName,
                player.CommonName,
                string.Join(" ", new[] { player.FirstName, player.LastName }
                    .Where(value => !string.IsNullOrWhiteSpace(value))))
                ?? $"player-{player.Id}";
        }

        private static string GetRequiredName(string? value, string entityName, long id)
        {
            return string.IsNullOrWhiteSpace(value)
                ? $"{entityName}-{id}"
                : value.Trim();
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values
                .Select(NullIfWhiteSpace)
                .FirstOrDefault(value => value != null);
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static int? NullIfZero(int value)
        {
            return value == 0 ? null : value;
        }

        private static int? NullIfZero(int? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static long? NullIfZero(long value)
        {
            return value == 0 ? null : value;
        }

        private static long? NullIfZero(long? value)
        {
            return value.GetValueOrDefault() == 0 ? null : value;
        }

        private static int? ToInt32OrNull(long? value)
        {
            if (!value.HasValue || value.Value == 0)
            {
                return null;
            }

            return value.Value is > int.MaxValue or < int.MinValue
                ? null
                : Convert.ToInt32(value.Value);
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

        private static NpgsqlParameter TextParameter(string name, string? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Text)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter BooleanParameter(string name, bool? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Boolean)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter DateParameter(string name, DateOnly? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Date)
            {
                Value = value ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter TimestampTzParameter(string name, DateTime? value)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.TimestampTz)
            {
                Value = NormalizeUtc(value) ?? (object)DBNull.Value
            };
        }

        private static NpgsqlParameter Parameter(string name, object? value)
        {
            return new NpgsqlParameter(name, value ?? DBNull.Value);
        }
    }
}
