using System.Globalization;
using Microsoft.Extensions.Configuration;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.V3;
using PreOddsApi.ExternalApis.Analytics;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace SportMonks.Football.FixtureWorker.Services
{
    public class FootballWorkerService : BackgroundService
    {
        private static class ScheduleKey
        {
            public const string Reference = "worker.football.reference";
            public const string Standings = "worker.football.standings";
            public const string Transfers = "worker.football.transfers";
            public const string TvStations = "worker.football.tv-stations";
            public const string News = "worker.football.news";
            // FixtureLive: livescores/latest tick (only fixtures changed in the
            // last few seconds). FixtureToday: full today-window fetch that
            // discovers fixtures whose state hasn't changed yet (kickoff in 30
            // min, etc.) — runs in the pulse tier rather than every live tick.
            public const string FixtureLive = "worker.football.fixture.live";
            public const string FixtureToday = "worker.football.fixture.today";
            public const string FixtureBacklog = "worker.football.fixture.backlog";
            // PlayerReference: bulk player/coach/rival pull is 800+ paginated
            // requests against the `player` rate-limit bucket. Gating it on a
            // weekly interval keeps the rest of the nightly tier inside the
            // 3000/hour budget.
            public const string PlayerReference = "worker.football.player-reference";
            public const string PrematchOdds = "worker.football.prematch-odds";
            public const string InplayOdds = "worker.football.inplay-odds";
            public const string Analytics = "worker.football.analytics";
            // OddOutcomeFinalize: stamps prematch_odds_current.winning for
            // fixtures that recently transitioned to a final state. Runs at
            // a tighter interval than the hourly analytics tier so the
            // historical odd grading lands within minutes of FT, while the
            // live SELECT still shows running winning via odds.evaluate_outcome.
            public const string OddOutcomeFinalize = "worker.football.odd-outcome-finalize";
            // NightlySnapshot: fires once per day after 03:00 UTC and runs
            // the full analytics rebuild chain (season + team + player +
            // outcome-finalizer w/ wide lookback + snapshot regenerate).
            // Separate from the hourly Analytics tier so the heavy nightly
            // job has its own visibility + retry semantics.
            public const string NightlySnapshot = "worker.football.nightly-snapshot";
            public const string AccountPurge = "worker.football.account-purge";
            public const string ExpectedXg = "worker.football.expected-xg";
        }

        private static readonly string[] FixtureSyncIncludes =
        [
            "sport",
            "league",
            "season",
            "stage",
            "group",
            "round",
            "state",
            "venue",
            "participants",
            "scores",
            "periods",
            "events",
            "statistics",
            "lineups",
            "lineups.details.type",
            "formations",
            "referees"
        ];

        // Minimum include set for the 10-second live tick. Trimmed from
        // FixtureSyncIncludes (the full set used by pulse + nightly + the
        // backlog/today calls) down to just the fields that drift on a
        // sub-minute cadence during a live match: state machine, scores
        // (current + per-half), per-period clock, events (goals / cards /
        // subs), and statistics. Participants is needed to map scores
        // back to home / away.
        //
        // Everything else (lineups, formations, sidelined, referees,
        // venue, league, season / stage / round / group meta, tv
        // stations, weather, news, timeline trends) is refreshed by the
        // 30-minute pulse tier — none of those drift between minute X
        // and minute X+1 of the same fixture, so paying the bandwidth +
        // JSON parse + DB-write cost on every 10s tick was waste that
        // also starved the live cycle (response large enough to slow
        // the tick body well past its 10s budget).
        //
        // Writer upserts already use COALESCE(excluded.X, table.X), so a
        // null arriving here doesn't blank out the previously-stored
        // lineup / formation / referee values.
        private static readonly string[] FixtureLiveTickIncludes =
        [
            "state",
            "scores",
            "participants",
            "periods",
            "events",
            "statistics"
        ];

        private static readonly string[] FixtureSidelinedIncludes =
        [
            "sidelined",
            "sidelined.player",
            "sidelined.type",
            "sidelined.participant",
            "sidelined.sideline"
        ];

        private static readonly string[] TvStationIncludes =
        [
            "countries"
        ];

        private static readonly string[] PlayerReferenceIncludes =
        [
            "sport",
            "country",
            "city",
            "nationality"
        ];

        private static readonly string[] CoachReferenceIncludes =
        [
            "country",
            "player"
        ];

        private static readonly string[] RivalReferenceIncludes =
        [
            "team",
            "rival"
        ];

        private static readonly string[] TeamSquadIncludes =
        [
            "team",
            "player"
        ];

        private static readonly string[] StandingIncludes =
        [
            "participant",
            "sport",
            "league",
            "season",
            "stage",
            "group",
            "round",
            "rule.type",
            "details.type",
            "form"
        ];

        private static readonly string[] TopScorerIncludes =
        [
            "season",
            "stage",
            "player",
            "participant",
            "type"
        ];

        private static readonly string[] TransferIncludes =
        [
            "sport",
            "player",
            "type",
            "fromTeam",
            "toTeam",
            "position",
            "detailedPosition"
        ];

        private static readonly string[] NewsIncludes =
        [
            "lines"
        ];

        private readonly ILogger<FootballWorkerService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISyncJobScheduler _scheduler;
        private readonly ISportMonksSyncRunner _syncRunner;
        private readonly ISportMonksApiClient _apiClient;
        private readonly ISportMonksCompetitionReferenceWriter _competitionReferenceWriter;
        private readonly ISportMonksFootballCoreReferenceWriter _footballCoreReferenceWriter;
        private readonly ISportMonksFixtureCoreWriter _fixtureCoreWriter;
        private readonly ISportMonksFixtureEventStatisticWriter _fixtureEventStatisticWriter;
        private readonly ISportMonksFixtureLineupFormationWriter _fixtureLineupFormationWriter;
        private readonly ISportMonksFixtureRefereeWriter _fixtureRefereeWriter;
        private readonly ISportMonksPlayerCoachSquadRivalWriter _playerCoachSquadRivalWriter;
        private readonly ISportMonksStandingTopScorerWriter _standingTopScorerWriter;
        private readonly ISportMonksTransferSidelinedWriter _transferSidelinedWriter;
        private readonly ISportMonksFixtureMediaWeatherWriter _fixtureMediaWeatherWriter;
        private readonly ISportMonksFixtureTrendCommentaryWriter _fixtureTrendCommentaryWriter;
        private readonly ISportMonksNewsWriter _newsWriter;
        private readonly ISportMonksPrematchOddsWriter _prematchOddsWriter;
        private readonly ISportMonksInplayOddsWriter _inplayOddsWriter;
        private readonly ISportMonksPredictionsWriter _predictionsWriter;
        private readonly ISportMonksValueBetsWriter _valueBetsWriter;
        private readonly ISportMonksFixtureExpectedGoalsWriter _expectedGoalsWriter;
        private readonly ISportMonksMatchFactsWriter _matchFactsWriter;
        private readonly IAnalyticsEngine _analyticsEngine;
        private readonly IFixtureLiveBridge _liveBridge;
        private readonly PreOddsApi.ExternalApis.Accounts.IAccountPurgeService _accountPurge;
        private readonly PreOddsApi.ExternalApis.Notifications.IEmailService _emailService;
        // Per-day retry bookkeeping for the nightly snapshot. Once an
        // attempt fails the worker waits NightlySnapshot:RetryDelayMinutes
        // before trying again; if the retry also fails an admin email
        // goes out and the job is marked done-for-today.
        private DateTimeOffset? _nightlySnapshotNextRetryAt;
        private int _nightlySnapshotAttemptCount;
        private DateOnly _nightlySnapshotAttemptDate;
        // UTC date of the last day we successfully ran (or gave up on
        // after retries). Tracks "we already handled today" without
        // depending on the interval-based ShouldRun, which drifts the
        // run time earlier each day. Persisted in scheduler too via
        // RecordRun, but cached here for cheap per-tick checks.
        private DateOnly _nightlySnapshotLastRunDate;

        private List<League> _cachedLeagues = [];

        public FootballWorkerService(
            ILogger<FootballWorkerService> logger,
            IConfiguration configuration,
            ISyncJobScheduler scheduler,
            ISportMonksSyncRunner syncRunner,
            ISportMonksApiClient apiClient,
            ISportMonksCompetitionReferenceWriter competitionReferenceWriter,
            ISportMonksFootballCoreReferenceWriter footballCoreReferenceWriter,
            ISportMonksFixtureCoreWriter fixtureCoreWriter,
            ISportMonksFixtureEventStatisticWriter fixtureEventStatisticWriter,
            ISportMonksFixtureLineupFormationWriter fixtureLineupFormationWriter,
            ISportMonksFixtureRefereeWriter fixtureRefereeWriter,
            ISportMonksPlayerCoachSquadRivalWriter playerCoachSquadRivalWriter,
            ISportMonksStandingTopScorerWriter standingTopScorerWriter,
            ISportMonksTransferSidelinedWriter transferSidelinedWriter,
            ISportMonksFixtureMediaWeatherWriter fixtureMediaWeatherWriter,
            ISportMonksFixtureTrendCommentaryWriter fixtureTrendCommentaryWriter,
            ISportMonksNewsWriter newsWriter,
            ISportMonksPrematchOddsWriter prematchOddsWriter,
            ISportMonksInplayOddsWriter inplayOddsWriter,
            ISportMonksPredictionsWriter predictionsWriter,
            ISportMonksValueBetsWriter valueBetsWriter,
            ISportMonksFixtureExpectedGoalsWriter expectedGoalsWriter,
            ISportMonksMatchFactsWriter matchFactsWriter,
            IAnalyticsEngine analyticsEngine,
            IFixtureLiveBridge liveBridge,
            PreOddsApi.ExternalApis.Accounts.IAccountPurgeService accountPurge,
            PreOddsApi.ExternalApis.Notifications.IEmailService emailService)
        {
            _logger = logger;
            _configuration = configuration;
            _scheduler = scheduler;
            _syncRunner = syncRunner;
            _apiClient = apiClient;
            _competitionReferenceWriter = competitionReferenceWriter;
            _footballCoreReferenceWriter = footballCoreReferenceWriter;
            _fixtureCoreWriter = fixtureCoreWriter;
            _fixtureEventStatisticWriter = fixtureEventStatisticWriter;
            _fixtureLineupFormationWriter = fixtureLineupFormationWriter;
            _fixtureRefereeWriter = fixtureRefereeWriter;
            _playerCoachSquadRivalWriter = playerCoachSquadRivalWriter;
            _standingTopScorerWriter = standingTopScorerWriter;
            _transferSidelinedWriter = transferSidelinedWriter;
            _fixtureMediaWeatherWriter = fixtureMediaWeatherWriter;
            _fixtureTrendCommentaryWriter = fixtureTrendCommentaryWriter;
            _newsWriter = newsWriter;
            _prematchOddsWriter = prematchOddsWriter;
            _inplayOddsWriter = inplayOddsWriter;
            _predictionsWriter = predictionsWriter;
            _valueBetsWriter = valueBetsWriter;
            _expectedGoalsWriter = expectedGoalsWriter;
            _matchFactsWriter = matchFactsWriter;
            _analyticsEngine = analyticsEngine;
            _liveBridge = liveBridge;
            _accountPurge = accountPurge;
            _emailService = emailService;
            _nightlySnapshotAttemptDate = DateOnly.MinValue;
            _nightlySnapshotLastRunDate = DateOnly.MinValue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Football worker starting up: live ({Live}s) + pulse ({Pulse}s) + nightly ({Nightly}s) tiers.",
                GetInteger("SportMonksWorkerSettings:LiveIntervalSeconds", 30),
                GetInteger("SportMonksWorkerSettings:PulseIntervalSeconds", 1800),
                GetInteger("SportMonksWorkerSettings:NightlyIntervalSeconds", 86400));

            // Three independent parallel loops so a slow tick on one tier
            // doesn't block the others. Each catches its own exceptions.
            var live = RunTierLoopAsync(
                "live",
                GetInteger("SportMonksWorkerSettings:LiveIntervalSeconds", 30),
                RunLiveTickAsync,
                stoppingToken);
            var pulse = RunTierLoopAsync(
                "pulse",
                GetInteger("SportMonksWorkerSettings:PulseIntervalSeconds", 1800),
                RunPulseTickAsync,
                stoppingToken);
            var nightly = RunTierLoopAsync(
                "nightly",
                GetInteger("SportMonksWorkerSettings:NightlyIntervalSeconds", 86400),
                RunNightlyTickAsync,
                stoppingToken);

            await Task.WhenAll(live, pulse, nightly);
        }

        private async Task RunTierLoopAsync(
            string tierName,
            int intervalSeconds,
            Func<CancellationToken, Task> tick,
            CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(Math.Max(5, intervalSeconds));
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("[{Tier}] tick at {Time}", tierName, DateTimeOffset.UtcNow);
                try
                {
                    await tick(stoppingToken);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "[{Tier}] tick failed: {Message}", tierName, exc.Message);
                }

                try
                {
                    await Task.Delay(interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunLiveTickAsync(CancellationToken cancellationToken)
        {
            // Tight loop driven by livescores/latest — only fixtures whose state
            // changed in the last few seconds come back, so the per-tick payload
            // stays small even with a wide league portfolio.
            await MaybeRunLivescoresLatestAsync(cancellationToken);
            await MaybeRunLatestInplayOddsAsync(cancellationToken);
        }

        private async Task RunPulseTickAsync(CancellationToken cancellationToken)
        {
            // Mid-frequency catch-up. Fixture-today picks up brand-new fixtures
            // and ones that haven't ticked yet (kickoff in 30 min, etc.) — the
            // live tier above only sees fixtures with a recent state change.
            // TV stations is also small + idempotent enough to live here:
            // letting it ride alongside the nightly chain was getting it
            // skipped whenever an earlier nightly job deadlocked.
            await MaybeRunFixtureTodayAsync(cancellationToken);
            await MaybeRunTvStationsAsync(cancellationToken);
            await MaybeRunExpectedXgAsync(cancellationToken);
            await MaybeRunNewsAsync(cancellationToken);
            await MaybeRunStandingsAsync(cancellationToken);
            await MaybeRunLatestPrematchOddsAsync(cancellationToken);
        }

        private async Task RunNightlyTickAsync(CancellationToken cancellationToken)
        {
            // Heavy lifting. Reference data, transfers, the full backlog
            // window so every finished match settles, then analytics.
            //
            // Each step lives in its own try/catch because the nightly
            // chain is once-per-day: if reference-data deadlocks on the
            // `football.teams` index (it can race the live tier's fixture
            // upserts), letting that propagate would skip every later
            // step — transfers, analytics, account-purge — for the next
            // 24 hours.
            await RunNightlyStepAsync("reference",       MaybeRunReferenceDataAsync, cancellationToken);
            await RunNightlyStepAsync("transfers",       MaybeRunTransfersAsync,     cancellationToken);
            await RunNightlyStepAsync("fixture-backlog", MaybeRunFixtureBacklogAsync, cancellationToken);
            await RunNightlyStepAsync("analytics",       MaybeRunAnalyticsAsync,     cancellationToken);
            await RunNightlyStepAsync("account-purge",   MaybeRunAccountPurgeAsync,  cancellationToken);
        }

        private async Task RunNightlyStepAsync(
            string name,
            Func<CancellationToken, Task> step,
            CancellationToken cancellationToken)
        {
            try
            {
                await step(cancellationToken);
            }
            catch (Exception exc)
            {
                _logger.LogError(
                    exc,
                    "[nightly:{Step}] step failed: {Message}",
                    name,
                    exc.Message);
            }
        }

        private async Task MaybeRunReferenceDataAsync(CancellationToken cancellationToken)
        {
            var interval = GetInteger("SportMonksWorkerSettings:ReferenceDataIntervalSeconds", 3600);
            if (!_scheduler.ShouldRun(ScheduleKey.Reference, interval))
                return;

            await ExecuteStates(cancellationToken);
            await ExecuteVenues(cancellationToken);
            var leagues = await ExecuteLeague(cancellationToken);
            var teams = await ExecuteTeams(cancellationToken);
            await ExecutePlayerCoachSquadRivalReferences(teams, cancellationToken);
            _cachedLeagues = leagues;
            _scheduler.RecordRun(ScheduleKey.Reference);
        }

        private async Task MaybeRunStandingsAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksStandingSync:Enabled", false))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:StandingsIntervalSeconds", 1800);
            if (!_scheduler.ShouldRun(ScheduleKey.Standings, interval))
                return;

            if (_cachedLeagues.Count == 0)
            {
                _logger.LogWarning("Standings sync skipped: no cached leagues available yet.");
                return;
            }

            await ExecuteStandingTopScorerReferences(_cachedLeagues, cancellationToken);
            _scheduler.RecordRun(ScheduleKey.Standings);
        }

        private async Task MaybeRunTransfersAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksTransferSidelinedSync:Enabled", false) ||
                !GetBoolean("SportMonksTransferSidelinedSync:SyncLatestTransfers", true))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:TransfersIntervalSeconds", 3600);
            if (!_scheduler.ShouldRun(ScheduleKey.Transfers, interval))
                return;

            await ExecuteTransferReferences(cancellationToken);
            _scheduler.RecordRun(ScheduleKey.Transfers);
        }

        private async Task MaybeRunExpectedXgAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksExpectedXgSync:Enabled", false) ||
                !GetBoolean("SportMonksExpectedXgSync:SyncFixtureXg", true))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:ExpectedXgIntervalSeconds", 1800);
            if (!_scheduler.ShouldRun(ScheduleKey.ExpectedXg, interval))
                return;

            await ExecuteExpectedXgAsync(cancellationToken);
            _scheduler.RecordRun(ScheduleKey.ExpectedXg);
        }

        private async Task ExecuteExpectedXgAsync(CancellationToken cancellationToken)
        {
            // The xG endpoint isn't fixture-scoped; one paginated call
            // returns rows for every fixture SportMonks tracks expected
            // goals for. Walk pages with the standard sync runner and
            // batch-upsert per page so a transient failure mid-walk only
            // loses the in-flight page.
            var request = SportMonksApiRequest.Create("expected/fixtures");
            var rows = (await _syncRunner.GetAllAsync<FixtureExpectedGoals>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.expected-fixtures",
                    "football.fixture_expected_goals",
                    "Sync SportMonks football expected-goals per fixture."),
                request,
                cancellationToken: cancellationToken)).ToList();

            await _expectedGoalsWriter.UpsertExpectedGoalsAsync(rows, cancellationToken);
        }

        private async Task MaybeRunTvStationsAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksFixtureMediaWeatherSync:Enabled", false) ||
                !GetBoolean("SportMonksFixtureMediaWeatherSync:SyncTvStations", true))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:TvStationsIntervalSeconds", 3600);
            if (!_scheduler.ShouldRun(ScheduleKey.TvStations, interval))
                return;

            await ExecuteTvStationReferences(cancellationToken);
            _scheduler.RecordRun(ScheduleKey.TvStations);
        }

        private async Task MaybeRunNewsAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksNewsSync:Enabled", false))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:NewsIntervalSeconds", 900);
            if (!_scheduler.ShouldRun(ScheduleKey.News, interval))
                return;

            await ExecuteNewsReferences(cancellationToken);
            _scheduler.RecordRun(ScheduleKey.News);
        }

        private async Task MaybeRunLivescoresLatestAsync(CancellationToken cancellationToken)
        {
            // /livescores/latest returns ONLY fixtures changed in the last 10
            // seconds (SportMonks's documented contract — see
            // docs.sportmonks.com/football/endpoints-and-entities/endpoints/
            // livescores). That makes the natural polling cadence 10 seconds:
            // anything slower opens a blind spot where a fixture transition
            // (minute increment, score, state) happens AND then doesn't change
            // again before the next poll, dropping that transition forever.
            //
            // We previously ran on 30s which lost ~67% of the API window. The
            // user-visible symptom was the live minute clock drifting several
            // minutes behind reality. With 10s the worst-case lag between a
            // SportMonks update and our DB row is ~10s + transport.
            //
            // Subscriptions without livescores access return 403 — we fall
            // back to fixtures/between/today/today so the live tier still
            // produces updates (one heavier call instead of a delta).
            if (!GetBoolean("SportMonksFixtureLiveSync:Enabled",
                    GetBoolean("SportMonksFixtureSync:Enabled", false)))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:FixtureLiveIntervalSeconds", 30);
            if (!_scheduler.ShouldRun(ScheduleKey.FixtureLive, interval))
                return;

            const string endpoint = "livescores/latest";
            // Live tick now optionally includes trends + pressure so the
            // mobile AttackMomentumCard updates in (near) real-time
            // instead of waiting for the 30-min pulse refresh. The
            // writer was refactored to per-fixture transactions for
            // exactly this reason — the previous single-big-transaction
            // shape OOM'd at this cadence. Falls back to the trimmed
            // set when SportMonksFixtureTimelineSync:Enabled=false.
            var request = SportMonksApiRequest.Create(endpoint)
                .WithInclude(BuildLiveTickIncludes().ToArray());

            var timezone = NullIfWhiteSpace(_configuration["SportMonksFixtureLiveSync:Timezone"])
                ?? NullIfWhiteSpace(_configuration["SportMonksFixtureSync:Timezone"]);
            if (timezone != null)
                request.WithTimezone(timezone);

            try
            {
                var fixtures = (await _syncRunner.GetAllAsync<Fixture>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.livescores.latest",
                        "football.fixture",
                        "Sync SportMonks fixtures whose state changed in the last few seconds."),
                    request,
                    cursorKey: endpoint,
                    cancellationToken: cancellationToken)).ToList();

                if (fixtures.Count > 0)
                    await ProcessFixturesAsync(fixtures, "live", cancellationToken);
            }
            catch (SportMonksApiException ex) when ((int)ex.StatusCode == 403)
            {
                _logger.LogWarning(
                    "livescores/latest returned 403 (subscription doesn't include it); falling back to fixtures/between/today.");
                await ExecuteFixtureWindow(0, 0, timezone, "live", cancellationToken);
            }

            _scheduler.RecordRun(ScheduleKey.FixtureLive);

            // Piggyback the outcome finalizer on the live tier so the moment
            // a fixture flips to FT/AET/PEN the next live tick stamps its
            // prematch_odds_current.winning rows. The finalizer has its own
            // schedule key + interval so it doesn't fire on every 30-second
            // live pulse — default cadence is 5 minutes, fast enough to
            // close the gap without burning Postgres CPU.
            await MaybeRunOddOutcomeFinalizeAsync(cancellationToken);
            // Nightly full-rebuild is also gated through the live tier so
            // worker restarts pick it up within 30 seconds of the target
            // hour. The check itself short-circuits 23h59m of the day.
            await MaybeRunNightlySnapshotAsync(cancellationToken);
            // Season-backfill walks past dates day-by-day to populate
            // historical fixtures + odds when a subscription upgrade
            // opens up old windows. Only runs when explicitly enabled
            // via env; processes a couple of days per live tick so it
            // doesn't starve the rest of the live pipeline.
            await MaybeRunSeasonBackfillAsync(cancellationToken);
            // Disk + long-query health monitor — emails admin when the
            // disk fills past the threshold or a query has been pinning
            // postgres for longer than the alert window. Self-throttles
            // via _lastHealthAlertAt so we don't spam during an outage.
            await MaybeRunHealthMonitorAsync(cancellationToken);
        }

        private async Task MaybeRunNightlySnapshotAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("NightlySnapshot:Enabled", true))
                return;

            // Calendar-day-based scheduling. Fires once per UTC date at
            // the configured target hour. Drift-resistant: a worker
            // started mid-afternoon today won't run again until 03:00
            // UTC tomorrow (vs. the old 23h interval which slid earlier
            // each day). The hour gate keeps the run pinned to roughly
            // the target hour.
            var targetHourUtc = GetInteger("NightlySnapshot:TargetHourUtc", 3);
            var nowUtc = DateTime.UtcNow;
            var todayUtc = DateOnly.FromDateTime(nowUtc);
            // Narrow [target, target+1) window. The earlier "hour >= target"
            // check would happily fire on a worker restart any time of day
            // after 03:00 UTC (e.g. a 23:30 restart with no scheduler record
            // triggered the full rebuild at the wrong end of the night).
            // Pinning to a single hour keeps the run cost predictable + off
            // the daytime traffic curve. Trade-off: if the worker is down
            // for the entire [3,4) window we miss the day — admin endpoint
            // POST /api/v3/admin/analytics/snapshot/rebuild covers that hole
            // on demand.
            if (nowUtc.Hour < targetHourUtc || nowUtc.Hour >= targetHourUtc + 1) return;

            // Seed the local cache from the scheduler on first tick so
            // a worker restart doesn't re-fire today's run. If
            // ShouldRun-based persistence ran the job earlier today,
            // GetLastRunUtc returns that timestamp; convert to UTC
            // date and short-circuit if it's today.
            if (_nightlySnapshotLastRunDate == DateOnly.MinValue)
            {
                var lastRun = _scheduler.GetLastRunUtc(ScheduleKey.NightlySnapshot);
                if (lastRun.HasValue)
                    _nightlySnapshotLastRunDate = DateOnly.FromDateTime(lastRun.Value.UtcDateTime);
            }
            if (_nightlySnapshotLastRunDate >= todayUtc) return;

            // Reset attempt counter for a new day.
            if (_nightlySnapshotAttemptDate != todayUtc)
            {
                _nightlySnapshotAttemptDate = todayUtc;
                _nightlySnapshotAttemptCount = 0;
                _nightlySnapshotNextRetryAt = null;
            }

            // If we're sitting in a backoff window after a previous fail
            // wait until the retry time arrives.
            if (_nightlySnapshotNextRetryAt.HasValue
                && DateTimeOffset.UtcNow < _nightlySnapshotNextRetryAt.Value)
                return;

            _nightlySnapshotAttemptCount++;
            var attempt = _nightlySnapshotAttemptCount;
            // Capture the actual start time so RecordRun can persist a
            // real duration (the legacy RecordRun overload stamped now()
            // for both started + completed, which made the admin grid
            // useless — every run showed 0s).
            var startedAt = DateTimeOffset.UtcNow;
            _logger.LogInformation(
                "NightlySnapshot attempt {Attempt} starting at {NowUtc}",
                attempt, startedAt);

            try
            {
                await _analyticsEngine.RunSeasonStatsAsync(cancellationToken);
                await _analyticsEngine.RunSeasonTeamStatsAsync(cancellationToken);
                await _analyticsEngine.RunSeasonPlayerStatsAsync(cancellationToken);
                // Lookback was 24 * 365 (one year), which produced a
                // multi-million-row join every nightly run and could
                // pin postgres for 20+ minutes. Product intent is "o
                // günün oranları için analiz" — finalize whatever
                // settled today + a safety buffer for late-arriving
                // matches. 72h (3 days) is plenty: anything older than
                // that already has its `winning` flag stamped from a
                // previous run, the finalizer is idempotent.
                var finalizeLookbackHours = GetInteger(
                    "NightlySnapshot:FinalizeLookbackHours", 72);
                var finalized = await _analyticsEngine.RunOddOutcomeFinalizerAsync(
                    finalizeLookbackHours, cancellationToken);
                var snapshotRows = await _analyticsEngine.RunOddAnalysisSnapshotsAsync(
                    cancellationToken);
                // Housekeeping — delete old api_requests log rows + VACUUM
                // ANALYZE the churn tables. Runs after the snapshot rebuild
                // so the freshly-written analytics rows benefit from the
                // VACUUM pass too. Doesn't throw on partial failure; the
                // engine logs each table and we keep the snapshot result
                // as the source-of-truth for "did the nightly run".
                var apiRequestRetentionDays = GetInteger(
                    "NightlySnapshot:ApiRequestRetentionDays", 60);
                var prunedApiRequests = await _analyticsEngine.RunMaintenanceCleanupAsync(
                    apiRequestRetentionDays, cancellationToken);

                _logger.LogInformation(
                    "NightlySnapshot success on attempt {Attempt}: outcomes_finalized={Finalized} snapshot_rows={Rows} pruned_api_requests={Pruned} (lookback={LookbackHours}h)",
                    attempt, finalized, snapshotRows, prunedApiRequests, finalizeLookbackHours);
                // Persist with the real metadata so the admin grid shows
                // a duration + row count. snapshotRows is the canonical
                // "did we produce useful output" number.
                _scheduler.RecordRun(
                    ScheduleKey.NightlySnapshot,
                    startedAt,
                    status: "success",
                    itemsCount: snapshotRows);
                _nightlySnapshotLastRunDate = todayUtc;
                _nightlySnapshotNextRetryAt = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "NightlySnapshot attempt {Attempt} failed.", attempt);

                var maxRetries = GetInteger("NightlySnapshot:MaxRetries", 1);
                if (attempt <= maxRetries)
                {
                    var delayMinutes = GetInteger("NightlySnapshot:RetryDelayMinutes", 5);
                    _nightlySnapshotNextRetryAt = DateTimeOffset.UtcNow.AddMinutes(delayMinutes);
                    _logger.LogWarning(
                        "NightlySnapshot scheduling retry in {Delay} min (next at {Next}).",
                        delayMinutes, _nightlySnapshotNextRetryAt);
                }
                else
                {
                    // Out of retries — mark the day done so we don't burn
                    // CPU all day, and email an admin so they can poke at
                    // it manually. Persist with status='failure' + the
                    // exception message so the admin grid surfaces what
                    // actually broke instead of a false "ok" pill.
                    _scheduler.RecordRun(
                        ScheduleKey.NightlySnapshot,
                        startedAt,
                        status: "failure",
                        itemsCount: null,
                        errorMessage: $"{ex.GetType().Name}: {ex.Message}");
                    _nightlySnapshotLastRunDate = todayUtc;
                    _nightlySnapshotNextRetryAt = null;
                    await TrySendNightlySnapshotFailureEmailAsync(ex, attempt, cancellationToken);
                }
            }
        }

        /// <summary>
        /// One-shot historical backfill driven by the
        /// SeasonBackfill:Enabled/FromDate/ToDate config. Walks the date
        /// range day-by-day, reusing the same fixtures/between/X/X path
        /// as the nightly backlog tier (full includes, idempotent
        /// upserts). Per-day checkpoints in sync.job_runs so a worker
        /// restart resumes from the last unfinished day. Processes only
        /// a couple of days per live tick so the live sync, finalizer
        /// and nightly snapshot keep running while the backfill drains
        /// in the background.
        ///
        /// After the window completes, fires the outcome finalizer +
        /// odd-analysis snapshot rebuild so HIT/ROI come alive
        /// immediately rather than waiting for the next 03:00 UTC tick.
        /// </summary>
        private async Task MaybeRunSeasonBackfillAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SeasonBackfill:Enabled", false))
                return;

            var fromDateStr = _configuration["SeasonBackfill:FromDate"];
            var toDateStr   = _configuration["SeasonBackfill:ToDate"];
            if (!DateOnly.TryParse(fromDateStr, out var fromDate) ||
                !DateOnly.TryParse(toDateStr,   out var toDate)   ||
                fromDate > toDate)
            {
                _logger.LogWarning(
                    "SeasonBackfill: invalid date window From={From} To={To}; skipping.",
                    fromDateStr, toDateStr);
                return;
            }

            // Embed the window in the scheduler key so changing
            // From/To re-runs cleanly without manually clearing the
            // sync.job_runs row.
            var windowKey = $"season.backfill:{fromDate:yyyyMMdd}:{toDate:yyyyMMdd}";
            if (_scheduler.GetLastRunUtc(windowKey).HasValue)
                return;

            var daysPerTick = Math.Max(1, GetInteger("SeasonBackfill:DaysPerTick", 2));
            var timezone    = NullIfWhiteSpace(_configuration["SeasonBackfill:Timezone"]);
            var endpointBase = GetFixtureByDateRangeEndpoint().TrimEnd('/');
            var includes     = BacklogLightIncludes;

            var processed = 0;
            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                if (processed >= daysPerTick) return;

                var dayKey = $"{windowKey}:{date:yyyy-MM-dd}";
                if (_scheduler.GetLastRunUtc(dayKey).HasValue) continue;

                var dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var endpoint = $"{endpointBase}/{dateStr}/{dateStr}";
                var request = SportMonksApiRequest.Create(endpoint)
                    .WithInclude(includes)
                    .WithQueryParameter("per_page", "10");
                if (timezone != null) request.WithTimezone(timezone);

                try
                {
                    var dayFixtures = (await _syncRunner.GetAllAsync<Fixture>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.fixtures.season-backfill",
                            "football.fixture",
                            "Season backfill: walk past dates day-by-day to populate history."),
                        request,
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    if (dayFixtures.Count > 0)
                        await ProcessFixturesAsync(dayFixtures, "backlog", cancellationToken);

                    _scheduler.RecordRun(dayKey);
                    processed++;
                    _logger.LogInformation(
                        "SeasonBackfill day {Date} done ({Count} fixtures).",
                        dateStr, dayFixtures.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "SeasonBackfill day {Date} failed; retrying on the next tick.", dateStr);
                    return;
                }
            }

            // We only reach here when every day in the window has a
            // RecordRun row (no `return` was hit early). Mark the
            // window complete + kick off the finalizer + snapshot
            // rebuild so analytics surfaces refresh in one go.
            _scheduler.RecordRun(windowKey);
            _logger.LogInformation(
                "SeasonBackfill window {From} → {To} COMPLETE. Rebuilding analytics.",
                fromDate, toDate);
            try
            {
                var graded = await _analyticsEngine.RunOddOutcomeFinalizerAsync(
                    24 * 365, cancellationToken);
                var snapshotRows = await _analyticsEngine.RunOddAnalysisSnapshotsAsync(
                    cancellationToken);
                _logger.LogInformation(
                    "SeasonBackfill post-process: graded={Graded} snapshot_rows={Rows}.",
                    graded, snapshotRows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "SeasonBackfill post-process failed; trigger the admin rebuild endpoint manually.");
            }
        }

        // Throttle bookkeeping for the disk / slow-query alerter so an
        // ongoing outage doesn't generate an email every 30s.
        private DateTimeOffset _lastHealthAlertAt = DateTimeOffset.MinValue;

        private async Task MaybeRunHealthMonitorAsync(CancellationToken cancellationToken)
        {
            // Single-shot per tick; the live tier runs every 30s so the
            // throttle below stretches an active alert to one email per
            // hour. Adjust HealthMonitor:AlertCooldownMinutes if the
            // signal-to-noise tradeoff changes.
            if (!GetBoolean("HealthMonitor:Enabled", true))
                return;

            var diskThreshold = GetInteger("HealthMonitor:DiskUsedPercentThreshold", 85);
            // Threshold for "this query is suspiciously long". Legitimate
            // batch ops (NightlySnapshot rebuild scans ~4 GB of
            // prematch_odds_current and aggregates by market × outcome ×
            // window — 5-10 min on the current host). 300s caught those
            // and spammed the inbox on a clean nightly run, so the floor
            // is now 900s — anything past 15 min is genuinely stuck and
            // worth a pager-style email. Tighten via env to 300 if a
            // future deploy needs aggressive alerting (e.g. during an
            // incident response window).
            var slowQueryThresholdSeconds = GetInteger("HealthMonitor:SlowQuerySeconds", 900);
            var cooldownMinutes = GetInteger("HealthMonitor:AlertCooldownMinutes", 60);

            if (DateTimeOffset.UtcNow - _lastHealthAlertAt < TimeSpan.FromMinutes(cooldownMinutes))
                return;

            try
            {
                // Disk check — DriveInfo("/") reflects the host root on
                // Linux Docker overlay2.
                double diskUsedPercent = 0;
                long diskFreeBytes = 0;
                long diskTotalBytes = 0;
                try
                {
                    var drive = new System.IO.DriveInfo("/");
                    if (drive.IsReady)
                    {
                        diskFreeBytes = drive.AvailableFreeSpace;
                        diskTotalBytes = drive.TotalSize;
                        diskUsedPercent = diskTotalBytes > 0
                            ? Math.Round((double)(diskTotalBytes - diskFreeBytes) / diskTotalBytes * 100.0, 2)
                            : 0;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "HealthMonitor disk probe failed (non-fatal).");
                }

                // Slow-query check — single SELECT against pg_stat_activity.
                var (longestSeconds, longestQuery) = await FetchLongestActiveQueryAsync(cancellationToken);

                var diskAlert = diskUsedPercent >= diskThreshold && diskTotalBytes > 0;
                var queryAlert = longestSeconds >= slowQueryThresholdSeconds;
                if (!diskAlert && !queryAlert)
                    return;

                var subject = "[TipsWall] Health alert — "
                    + (diskAlert ? "disk " : string.Empty)
                    + (diskAlert && queryAlert ? "+ " : string.Empty)
                    + (queryAlert ? "slow query " : string.Empty)
                    + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'");

                var body = new System.Text.StringBuilder();
                if (diskAlert)
                {
                    body.AppendLine($"DISK: {diskUsedPercent}% used "
                        + $"({FormatBytes(diskTotalBytes - diskFreeBytes)} / {FormatBytes(diskTotalBytes)})");
                    body.AppendLine($"Free: {FormatBytes(diskFreeBytes)}");
                    body.AppendLine($"Threshold: {diskThreshold}%");
                    body.AppendLine();
                }
                if (queryAlert)
                {
                    body.AppendLine($"SLOW QUERY: {longestSeconds:F0}s running");
                    body.AppendLine($"Threshold: {slowQueryThresholdSeconds}s");
                    body.AppendLine();
                    body.AppendLine("Query (first 500 chars):");
                    body.AppendLine(longestQuery is { Length: > 500 } ? longestQuery[..500] : longestQuery);
                    body.AppendLine();
                    body.AppendLine("To terminate from admin panel or psql:");
                    body.AppendLine("  select pg_terminate_backend(pid) from pg_stat_activity");
                    body.AppendLine("  where state='active' and now()-query_start > interval '5 min';");
                }

                var adminEmail = Environment.GetEnvironmentVariable("NIGHTLY_SNAPSHOT_ADMIN_EMAIL")
                    ?? _configuration["NightlySnapshot:AdminEmail"]
                    ?? "ethemserce@gmail.com";

                await _emailService.SendAsync(adminEmail, subject, body.ToString(), cancellationToken);
                _lastHealthAlertAt = DateTimeOffset.UtcNow;
                _logger.LogWarning(
                    "HealthMonitor alert sent: disk={DiskPercent}% query={LongestSeconds:F0}s",
                    diskUsedPercent, longestSeconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "HealthMonitor check failed (will retry next tick).");
            }
        }

        private async Task<(double seconds, string query)> FetchLongestActiveQueryAsync(
            CancellationToken cancellationToken)
        {
            var connectionString = Environment.GetEnvironmentVariable("PREODDS_POSTGRES_CONNECTION")
                ?? _configuration.GetConnectionString("PreOddsApiPostgresDb");
            if (string.IsNullOrWhiteSpace(connectionString)) return (0, string.Empty);

            const string sql = """
                select extract(epoch from now() - query_start)::float as seconds,
                       coalesce(left(query, 1000), '') as query_text
                from pg_stat_activity
                where state = 'active' and pid != pg_backend_pid()
                order by query_start
                limit 1;
                """;
            await using var conn = new Npgsql.NpgsqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);
            await using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            cmd.CommandTimeout = 10;
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return (0, string.Empty);
            return (reader.GetDouble(0), reader.IsDBNull(1) ? string.Empty : reader.GetString(1));
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            double v = bytes;
            string[] units = { "KB", "MB", "GB", "TB" };
            int i = -1;
            do { v /= 1024; i++; } while (v >= 1024 && i < units.Length - 1);
            return $"{v:F2} {units[i]}";
        }

        private async Task TrySendNightlySnapshotFailureEmailAsync(
            Exception ex, int attempts, CancellationToken ct)
        {
            var adminEmail = Environment.GetEnvironmentVariable("NIGHTLY_SNAPSHOT_ADMIN_EMAIL")
                ?? _configuration["NightlySnapshot:AdminEmail"]
                ?? "ethemserce@gmail.com";

            var subject = $"[TipsWall] Nightly snapshot failed after {attempts} attempt(s) — {DateTime.UtcNow:yyyy-MM-dd}";
            var body =
                $"The nightly analytics snapshot rebuild failed twice on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC.\n\n" +
                $"Final exception:\n{ex.GetType().FullName}: {ex.Message}\n\n" +
                $"Stack trace:\n{ex.StackTrace}\n\n" +
                "The job won't retry again today. Investigate, then re-run from the\n" +
                "admin endpoint:\n" +
                "  POST /api/v3/admin/analytics/snapshot/rebuild  X-Internal-Api-Key: ...\n";

            try
            {
                await _emailService.SendAsync(adminEmail, subject, body, ct);
                _logger.LogInformation(
                    "NightlySnapshot failure email sent to {Admin}.", adminEmail);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx,
                    "Failed to send NightlySnapshot failure email to {Admin}.", adminEmail);
            }
        }

        private async Task MaybeRunOddOutcomeFinalizeAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("OddOutcomeFinalize:Enabled", true))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:OddOutcomeFinalizeIntervalSeconds", 300);
            if (!_scheduler.ShouldRun(ScheduleKey.OddOutcomeFinalize, interval))
                return;

            var lookback = GetInteger("OddOutcomeFinalize:LookbackHours", 36);

            try
            {
                await _analyticsEngine.RunOddOutcomeFinalizerAsync(lookback, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OddOutcomeFinalize: stamping prematch winning failed; will retry next tick.");
            }

            _scheduler.RecordRun(ScheduleKey.OddOutcomeFinalize);
        }

        private async Task MaybeRunFixtureTodayAsync(CancellationToken cancellationToken)
        {
            // Today's full fixture list (with the configurable ± buffer). Pulse
            // tier owns this so brand-new fixtures and ones still 30 min from
            // kickoff get discovered without spamming SportMonks every live
            // tick. Falls back to the legacy SportMonksFixtureSync block.
            if (!GetBoolean("SportMonksFixtureLiveSync:Enabled",
                    GetBoolean("SportMonksFixtureSync:Enabled", false)))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:FixtureTodayIntervalSeconds", 1800);
            if (!_scheduler.ShouldRun(ScheduleKey.FixtureToday, interval))
                return;

            var daysBack = Math.Max(0, GetInteger("SportMonksFixtureLiveSync:DaysBack", 0));
            var daysForward = Math.Max(0, GetInteger("SportMonksFixtureLiveSync:DaysForward", 0));
            var timezone = NullIfWhiteSpace(_configuration["SportMonksFixtureLiveSync:Timezone"])
                ?? NullIfWhiteSpace(_configuration["SportMonksFixtureSync:Timezone"]);

            await ExecuteFixtureWindow(daysBack, daysForward, timezone, "today", cancellationToken);
            _scheduler.RecordRun(ScheduleKey.FixtureToday);
        }

        private async Task MaybeRunFixtureBacklogAsync(CancellationToken cancellationToken)
        {
            // Wide window in the nightly tier so freshly finished matches and
            // upcoming weeks all upsert with their includes. Falls back to the
            // legacy SportMonksFixtureSync block.
            if (!GetBoolean("SportMonksFixtureBacklogSync:Enabled",
                    GetBoolean("SportMonksFixtureSync:Enabled", false)))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:FixtureBacklogIntervalSeconds", 86400);
            if (!_scheduler.ShouldRun(ScheduleKey.FixtureBacklog, interval))
                return;

            var daysBack = Math.Max(0, GetInteger("SportMonksFixtureBacklogSync:DaysBack",
                GetInteger("SportMonksFixtureSync:DaysBack", 2)));
            var daysForward = Math.Max(0, GetInteger("SportMonksFixtureBacklogSync:DaysForward",
                GetInteger("SportMonksFixtureSync:DaysForward", 14)));
            var timezone = NullIfWhiteSpace(_configuration["SportMonksFixtureBacklogSync:Timezone"])
                ?? NullIfWhiteSpace(_configuration["SportMonksFixtureSync:Timezone"]);

            await ExecuteFixtureWindow(daysBack, daysForward, timezone, "backlog", cancellationToken);
            _scheduler.RecordRun(ScheduleKey.FixtureBacklog);
        }

        private async Task<List<League>> ExecuteLeague(CancellationToken cancellationToken)
        {
            var leagues = (await _syncRunner.GetAllAsync<League>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.leagues",
                    "competition.league",
                    "Sync SportMonks football leagues with sport, seasons, stages, rounds, and groups."),
                SportMonksApiRequest.Create("leagues")
                    .WithInclude(
                        "sport",
                        "seasons",
                        "currentSeason",
                        "stages",
                        "stages.rounds",
                        "stages.groups",
                        "seasons.groups"),
                cancellationToken: cancellationToken)).ToList();

            await _competitionReferenceWriter.UpsertLeaguesWithHierarchyAsync(leagues, cancellationToken);
            return leagues;
        }

        private async Task ExecuteStates(CancellationToken cancellationToken)
        {
            var states = (await _syncRunner.GetAllAsync<State>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.states",
                    "catalog.state",
                    "Sync SportMonks football states with type include."),
                SportMonksApiRequest.Create("states"),
                cancellationToken: cancellationToken)).ToList();

            await _footballCoreReferenceWriter.UpsertStatesAsync(states, cancellationToken);
        }

        private async Task ExecuteVenues(CancellationToken cancellationToken)
        {
            var venues = (await _syncRunner.GetAllAsync<Venue>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.venues",
                    "football.venue",
                    "Sync SportMonks football venues."),
                SportMonksApiRequest.Create("venues")
                    .WithInclude("country", "city"),
                cancellationToken: cancellationToken)).ToList();

            await _footballCoreReferenceWriter.UpsertVenuesAsync(venues, cancellationToken);
        }

        private async Task<List<Team>> ExecuteTeams(CancellationToken cancellationToken)
        {
            var teams = (await _syncRunner.GetAllAsync<Team>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.teams",
                    "football.team",
                    "Sync SportMonks football teams with sport and venue include."),
                SportMonksApiRequest.Create("teams")
                    .WithInclude("sport", "venue"),
                cancellationToken: cancellationToken)).ToList();

            await _footballCoreReferenceWriter.UpsertTeamsWithVenuesAsync(teams, cancellationToken);
            return teams;
        }

        private async Task ExecutePlayerCoachSquadRivalReferences(
            IReadOnlyCollection<Team> teams,
            CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksPlayerReferenceSync:Enabled", false))
                return;

            // Bulk /players is 800+ paginated calls against the `player`
            // rate-limit entity (3000/hour). Coaches + rivals add a few
            // hundred more on their respective entities. Gate the whole
            // group on a weekly cadence so the nightly tier stays inside
            // the budget; reference data here is slow-changing.
            var interval = GetInteger(
                "SportMonksWorkerSettings:PlayerReferenceIntervalSeconds",
                604800);
            if (!_scheduler.ShouldRun(ScheduleKey.PlayerReference, interval))
                return;

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncPlayers", true))
            {
                var players = (await _syncRunner.GetAllAsync<Player>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.players",
                        "football.player",
                        "Sync SportMonks football player reference data."),
                    SportMonksApiRequest.Create("players")
                        .WithInclude(PlayerReferenceIncludes)
                        .WithQueryParameter("per_page", "50"),
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertPlayersAsync(players, cancellationToken);
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncCoaches", true))
            {
                var coaches = (await _syncRunner.GetAllAsync<Coach>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.coaches",
                        "football.coach",
                        "Sync SportMonks football coach reference data."),
                    SportMonksApiRequest.Create("coaches")
                        .WithInclude(CoachReferenceIncludes)
                        .WithQueryParameter("per_page", "50"),
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertCoachesAsync(coaches, cancellationToken);
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncRivals", true))
            {
                var rivals = (await _syncRunner.GetAllAsync<Rival>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.rivals",
                        "football.team_rival",
                        "Sync SportMonks football team rival reference data."),
                    SportMonksApiRequest.Create("rivals")
                        .WithInclude(RivalReferenceIncludes)
                        .WithQueryParameter("per_page", "50"),
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertTeamRivalsAsync(rivals, cancellationToken);
            }

            if (GetBoolean("SportMonksPlayerReferenceSync:SyncTeamSquads", false))
                await ExecuteTeamSquads(teams, cancellationToken);

            _scheduler.RecordRun(ScheduleKey.PlayerReference);
        }

        private async Task ExecuteTeamSquads(
            IReadOnlyCollection<Team> teams,
            CancellationToken cancellationToken)
        {
            var maxTeamsPerRun = Math.Max(0, GetInteger("SportMonksPlayerReferenceSync:MaxSquadTeamsPerRun", 0));
            var teamList = teams
                .Where(team => team != null && team.Id > 0)
                .GroupBy(team => team.Id)
                .Select(group => group.Last())
                .OrderBy(team => team.Id)
                .ToList();

            if (maxTeamsPerRun > 0)
                teamList = teamList.Take(maxTeamsPerRun).ToList();

            foreach (var team in teamList)
            {
                var endpoint = $"squads/teams/{team.Id}";
                var teamSquads = (await _syncRunner.GetAllAsync<TeamSquad>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.team-squads.by-team",
                        "football.team_squad",
                        "Sync SportMonks football team squad rows by team."),
                    SportMonksApiRequest.Create(endpoint)
                        .WithInclude(TeamSquadIncludes)
                        .WithoutDefaultPagination(),
                    cursorKey: endpoint,
                    cancellationToken: cancellationToken)).ToList();

                await _playerCoachSquadRivalWriter.UpsertTeamSquadsAsync(teamSquads, cancellationToken);
            }
        }

        private async Task ExecuteStandingTopScorerReferences(
            IReadOnlyCollection<League> leagues,
            CancellationToken cancellationToken)
        {
            var seasons = GetStandingSyncSeasons(leagues);

            if (seasons.Count == 0)
            {
                _logger.LogWarning(
                    "SportMonks standings/top scorers sync is enabled, but no eligible seasons were found.");
                return;
            }

            if (GetBoolean("SportMonksStandingSync:SyncStandings", true))
            {
                foreach (var season in seasons)
                {
                    var endpoint = $"standings/seasons/{season.Id}";
                    var standings = (await _syncRunner.GetAllAsync<Standing>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.standings.by-season",
                            "competition.standing",
                            "Sync SportMonks football standings by season."),
                        SportMonksApiRequest.Create(endpoint)
                            .WithInclude(StandingIncludes)
                            .WithoutDefaultPagination(),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    await _standingTopScorerWriter.UpsertStandingsAsync(standings, cancellationToken);
                }
            }

            if (GetBoolean("SportMonksStandingSync:SyncTopScorers", true))
            {
                foreach (var season in seasons)
                {
                    var endpoint = $"topscorers/seasons/{season.Id}";
                    var topScorers = (await _syncRunner.GetAllAsync<TopScorer>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.top-scorers.by-season",
                            "competition.top_scorer",
                            "Sync SportMonks football top scorers by season."),
                        SportMonksApiRequest.Create(endpoint)
                            .WithInclude(TopScorerIncludes),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    await _standingTopScorerWriter.UpsertTopScorersAsync(topScorers, cancellationToken);
                }
            }
        }

        private IReadOnlyList<Season> GetStandingSyncSeasons(IEnumerable<League> leagues)
        {
            var currentOnly = GetBoolean("SportMonksStandingSync:CurrentSeasonsOnly", true);
            var maxSeasonsPerRun = Math.Max(0, GetInteger("SportMonksStandingSync:MaxSeasonsPerRun", 0));
            var seasons = ExtractStandingSyncSeasons(leagues)
                .Where(season => season != null && season.Id > 0)
                .Where(season => !currentOnly || season.IsCurrent)
                .GroupBy(season => season.Id)
                .Select(group => group.Last())
                .OrderByDescending(season => season.StartingAt ?? DateTime.MinValue)
                .ThenByDescending(season => season.Id)
                .ToList();

            if (maxSeasonsPerRun > 0)
                seasons = seasons.Take(maxSeasonsPerRun).ToList();

            return seasons;
        }

        private static IEnumerable<Season> ExtractStandingSyncSeasons(IEnumerable<League> leagues)
        {
            foreach (var league in leagues)
            {
                foreach (var season in league.Seasons ?? Enumerable.Empty<Season>())
                    yield return season;

                if (league.CurrentSeason != null)
                    yield return league.CurrentSeason;
            }
        }

        private async Task ExecuteTransferReferences(CancellationToken cancellationToken)
        {
            var order = NullIfWhiteSpace(_configuration["SportMonksTransferSidelinedSync:TransferOrder"]) ?? "desc";
            var transfers = (await _syncRunner.GetAllAsync<Transfer>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.transfers.latest",
                    "football.transfer",
                    "Sync latest SportMonks football transfers."),
                SportMonksApiRequest.Create("transfers/latest")
                    .WithInclude(TransferIncludes)
                    .WithQueryParameter("order", order),
                cancellationToken: cancellationToken)).ToList();

            await _transferSidelinedWriter.UpsertTransfersAsync(transfers, cancellationToken);
        }

        private async Task ExecuteTvStationReferences(CancellationToken cancellationToken)
        {
            var order = NullIfWhiteSpace(_configuration["SportMonksFixtureMediaWeatherSync:TvStationOrder"]) ?? "asc";
            var request = SportMonksApiRequest.Create("tv-stations")
                .WithQueryParameter("order", order);

            if (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncTvStationCountries", true))
                request.WithInclude(TvStationIncludes);

            var tvStations = (await _syncRunner.GetAllAsync<TvStation>(
                SportMonksSyncJobDefinition.Create(
                    "sportmonks.football.tv-stations",
                    "football.tv_station",
                    "Sync SportMonks football TV station reference data."),
                request,
                cancellationToken: cancellationToken)).ToList();

            await _fixtureMediaWeatherWriter.UpsertTvStationsAsync(tvStations, cancellationToken);
        }

        private async Task ExecuteNewsReferences(CancellationToken cancellationToken)
        {
            if (GetBoolean("SportMonksNewsSync:SyncAllPreMatchNews", false))
            {
                await ExecuteNewsEndpoint(
                    "sportmonks.football.news.pre-match",
                    GetConfiguredEndpoint("SportMonksUrls:preMatchNews", "news/pre-match"),
                    "Sync SportMonks pre-match news.",
                    cancellationToken);
            }

            if (GetBoolean("SportMonksNewsSync:SyncUpcomingPreMatchNews", false))
            {
                await ExecuteNewsEndpoint(
                    "sportmonks.football.news.pre-match.upcoming",
                    GetConfiguredEndpoint("SportMonksUrls:preMatchNewsUpcoming", "news/pre-match/upcoming"),
                    "Sync SportMonks pre-match news for upcoming fixtures.",
                    cancellationToken);
            }

            if (GetBoolean("SportMonksNewsSync:SyncAllPostMatchNews", false))
            {
                await ExecuteNewsEndpoint(
                    "sportmonks.football.news.post-match",
                    GetConfiguredEndpoint("SportMonksUrls:postMatchNews", "news/post-match"),
                    "Sync SportMonks post-match news.",
                    cancellationToken);
            }
        }

        private async Task ExecuteNewsEndpoint(
            string jobKey,
            string endpoint,
            string description,
            CancellationToken cancellationToken)
        {
            var request = SportMonksApiRequest.Create(endpoint);
            var order = NullIfWhiteSpace(_configuration["SportMonksNewsSync:Order"]);

            if (order != null)
                request.WithQueryParameter("order", order);

            if (GetBoolean("SportMonksNewsSync:IncludeLines", true))
                request.WithInclude(NewsIncludes);

            var news = (await _syncRunner.GetAllAsync<News>(
                SportMonksSyncJobDefinition.Create(
                    jobKey,
                    "football.news",
                    description),
                request,
                cursorKey: endpoint,
                cancellationToken: cancellationToken)).ToList();

            await _newsWriter.UpsertNewsAsync(news, cancellationToken);
        }

        // Include set for the nightly backlog window. We keep odds here
        // because /v3/football/odds/pre-match/latest only returns rows
        // that were UPDATED recently — fixtures that never had a first
        // odds row land never become "latest" and stay odds-less in the
        // DB, so the only reliable way to seed upcoming-fixture odds is
        // to ask for them inline with the fixture window.
        //
        // We drop lineups/formations/weather/trends/pressure to keep the
        // payload (and the raw_payloads INSERT) bounded; pulse re-fetches
        // those per-fixture for today's matches anyway.
        private static readonly string[] BacklogLightIncludes =
        [
            "sport",
            "league",
            "season",
            "stage",
            "round",
            "state",
            "venue",
            "participants",
            "scores",
            "events",
            "statistics",
            "referees",
            "odds",
            "odds.market",
            "odds.bookmaker",
        ];

        private async Task ExecuteFixtureWindow(
            int daysBack,
            int daysForward,
            string? timezone,
            string label,
            CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var fromDate = today.AddDays(-daysBack);
            var toDate = today.AddDays(daysForward);

            _logger.LogInformation(
                "Fixture {Label} sync started for dates {FromDate} through {ToDate}.",
                label,
                fromDate,
                toDate);

            // Backlog historically called `fixtures/between/{from}/{to}` as a
            // single multi-day window with full includes, but the response
            // for 7+ days × every league × odds was multi-MB and OOM'd the
            // worker (and then sigkilled postgres while it tried to INSERT
            // the raw_payloads blob). Loop per-day so each request stays
            // small — pagination still happens inside GetAllAsync per day.
            // Today / live windows are always single-day so the loop is a
            // no-op for them.
            var isBacklog = label == "backlog";
            var includes = isBacklog
                ? BacklogLightIncludes
                : BuildFixtureSyncIncludes().ToArray();
            var endpointBase = GetFixtureByDateRangeEndpoint().TrimEnd('/');
            // Backlog single-day responses can still spike past 1 GB when the
            // day carries 60+ fixtures (Saturday MLS + EPL weekend) — every
            // fixture brings 14+ embedded entities with odds. Force a small
            // per_page so pagination shards the JSON into bite-sized chunks.
            // 10 felt right in tests: a 100-fixture Saturday becomes 10 pages
            // of ~5-10 MB each, well under the worker's 1.28 G cap.
            var perPage = isBacklog ? "10" : null;

            for (var date = fromDate; date <= toDate; date = date.AddDays(1))
            {
                var dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                var endpoint = $"{endpointBase}/{dateStr}/{dateStr}";
                var request = SportMonksApiRequest.Create(endpoint)
                    .WithInclude(includes);
                if (perPage != null)
                    request.WithQueryParameter("per_page", perPage);
                if (timezone != null)
                    request.WithTimezone(timezone);

                var dayFixtures = (await _syncRunner.GetAllAsync<Fixture>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.fixtures.by-date-range",
                        "football.fixture",
                        "Sync SportMonks football fixtures across a date window."),
                    request,
                    cursorKey: endpoint,
                    cancellationToken: cancellationToken)).ToList();

                // Persist per-day so the next iteration starts with a clear
                // managed-object graph. Keeping every day's fixtures in RAM
                // until the end was the OOM trigger on 7-day windows.
                if (dayFixtures.Count > 0)
                    await ProcessFixturesAsync(dayFixtures, label, cancellationToken);
            }
        }

        private async Task ProcessFixturesAsync(
            IReadOnlyList<Fixture> fixtures,
            string label,
            CancellationToken cancellationToken)
        {
            await _fixtureCoreWriter.UpsertFixturesAsync(fixtures, cancellationToken);
            await _fixtureRefereeWriter.UpsertFixtureRefereesAsync(fixtures, cancellationToken);
            await _fixtureEventStatisticWriter.UpsertEventsAndStatisticsAsync(fixtures, cancellationToken);
            await _fixtureLineupFormationWriter.UpsertLineupsAndFormationsAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureSidelined())
                await _transferSidelinedWriter.UpsertFixtureSidelinedAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureMediaWeather())
                await _fixtureMediaWeatherWriter.UpsertFixtureMediaWeatherAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureTimeline())
                await _fixtureTrendCommentaryWriter.UpsertTrendsAndCommentariesAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureNews())
                await _newsWriter.UpsertFixtureNewsAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureOdds())
                await _prematchOddsWriter.UpsertPrematchOddsAsync(fixtures, cancellationToken);

            // Per-fixture odds SEED — fetches the full pre-match odds set
            // (every market × every line × every outcome) from
            // /odds/pre-match/fixtures/{id}. Necessary because /odds/pre-
            // match/latest only ships rows that updated recently, and
            // alternative O/U lines (0.5, 1.5, 3.5, 4.5, ...) are seeded
            // once at market open and never updated, so they never appear
            // in /latest. Without this seed our DB has only the 2.5 main
            // line. Runs on the same labels as predictions/value-bets to
            // keep call volume predictable (today + backlog tiers only,
            // not the 30s live tick).
            if (ShouldSyncFixtureOddsSeed(label))
                await UpsertPrematchOddsSeedForFixturesAsync(fixtures, cancellationToken);

            if (ShouldSyncFixturePredictions(label))
                await UpsertPredictionsForFixturesAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureValueBets(label))
                await UpsertValueBetsForFixturesAsync(fixtures, cancellationToken);

            if (ShouldSyncFixtureMatchFacts(label))
                await UpsertMatchFactsForFixturesAsync(fixtures, cancellationToken);

            // Live label broadcasts a SignalR push per fixture so subscribed
            // mobile clients refresh without polling. Today/backlog stay quiet
            // (waking up sleeping mobile clients for catch-up data is noise).
            if (string.Equals(label, "live", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var fixture in fixtures)
                {
                    await _liveBridge.NotifyFixtureUpdatedAsync(
                        fixture.Id,
                        $"fixture-{label}",
                        cancellationToken);
                }
            }
        }

        private IEnumerable<string> BuildFixtureSyncIncludes(bool allowOddsInclude = true)
        {
            foreach (var include in FixtureSyncIncludes)
                yield return include;

            if (ShouldSyncFixtureSidelined())
                foreach (var include in FixtureSidelinedIncludes)
                    yield return include;

            foreach (var include in BuildFixtureMediaWeatherIncludes())
                yield return include;

            foreach (var include in BuildFixtureTimelineIncludes())
                yield return include;

            foreach (var include in BuildFixtureNewsIncludes())
                yield return include;

            // /livescores/latest rejects the `odds` family with 422 even when
            // the subscription grants odds elsewhere — odds are reserved for
            // the standalone /odds/* endpoints and the fixtures/between path.
            if (allowOddsInclude)
                foreach (var include in BuildFixtureOddsIncludes())
                    yield return include;
        }

        private bool ShouldSyncFixtureSidelined()
        {
            return GetBoolean("SportMonksTransferSidelinedSync:Enabled", false) &&
                   GetBoolean("SportMonksTransferSidelinedSync:SyncFixtureSidelined", false);
        }

        private IEnumerable<string> BuildFixtureMediaWeatherIncludes()
        {
            if (!GetBoolean("SportMonksFixtureMediaWeatherSync:Enabled", false))
                yield break;

            if (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncFixtureTvStations", false))
                yield return "tvStations";

            if (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncWeatherReports", false))
                yield return "weatherReport";
        }

        private bool ShouldSyncFixtureMediaWeather()
        {
            return GetBoolean("SportMonksFixtureMediaWeatherSync:Enabled", false) &&
                   (GetBoolean("SportMonksFixtureMediaWeatherSync:SyncFixtureTvStations", false) ||
                    GetBoolean("SportMonksFixtureMediaWeatherSync:SyncWeatherReports", false));
        }

        // Live tick include set — minimal default + optional trends /
        // pressure when the timeline sync is enabled. We don't wrap
        // commentary here even when SyncCommentaries is on: commentary
        // payloads are large, change verbosely during a match, and the
        // mobile UI doesn't render them. Pulse tier picks them up at
        // its 30-min cadence which is enough for the niche cases.
        private IEnumerable<string> BuildLiveTickIncludes()
        {
            foreach (var include in FixtureLiveTickIncludes)
                yield return include;
            if (!GetBoolean("SportMonksFixtureTimelineSync:Enabled", false))
                yield break;
            if (GetBoolean("SportMonksFixtureTimelineSync:LiveTickTrends",
                    GetBoolean("SportMonksFixtureTimelineSync:SyncTrends", false)))
                yield return "trends";
            if (GetBoolean("SportMonksFixtureTimelineSync:LiveTickPressureTrends",
                    GetBoolean("SportMonksFixtureTimelineSync:SyncPressureTrends", false)))
                yield return "pressure";
        }

        private IEnumerable<string> BuildFixtureTimelineIncludes()
        {
            if (!GetBoolean("SportMonksFixtureTimelineSync:Enabled", false))
                yield break;

            if (GetBoolean("SportMonksFixtureTimelineSync:SyncTrends", false))
                yield return "trends";

            if (GetBoolean("SportMonksFixtureTimelineSync:SyncPressureTrends", false))
                yield return "pressure";

            if (GetBoolean("SportMonksFixtureTimelineSync:SyncCommentaries", false))
                yield return "comments";
        }

        private bool ShouldSyncFixtureTimeline()
        {
            return GetBoolean("SportMonksFixtureTimelineSync:Enabled", false) &&
                   (GetBoolean("SportMonksFixtureTimelineSync:SyncTrends", false) ||
                    GetBoolean("SportMonksFixtureTimelineSync:SyncPressureTrends", false) ||
                    GetBoolean("SportMonksFixtureTimelineSync:SyncCommentaries", false));
        }

        private IEnumerable<string> BuildFixtureNewsIncludes()
        {
            if (!GetBoolean("SportMonksNewsSync:Enabled", false))
                yield break;

            var includeLines = GetBoolean("SportMonksNewsSync:IncludeLines", true);

            if (GetBoolean("SportMonksNewsSync:SyncFixturePreMatchNews", false))
            {
                yield return "prematchNews";
                if (includeLines)
                    yield return "prematchNews.lines";
            }

            if (GetBoolean("SportMonksNewsSync:SyncFixturePostMatchNews", false))
            {
                yield return "postmatchNews";
                if (includeLines)
                    yield return "postmatchNews.lines";
            }
        }

        private bool ShouldSyncFixtureNews()
        {
            return GetBoolean("SportMonksNewsSync:Enabled", false) &&
                   (GetBoolean("SportMonksNewsSync:SyncFixturePreMatchNews", false) ||
                    GetBoolean("SportMonksNewsSync:SyncFixturePostMatchNews", false));
        }

        private IEnumerable<string> BuildFixtureOddsIncludes()
        {
            if (!ShouldSyncFixtureOdds())
                yield break;

            yield return "odds";

            if (GetBoolean("SportMonksPrematchOddsSync:IncludeMarket", true))
                yield return "odds.market";

            if (GetBoolean("SportMonksPrematchOddsSync:IncludeBookmaker", true))
                yield return "odds.bookmaker";
        }

        private bool ShouldSyncFixtureOdds()
        {
            return GetBoolean("SportMonksPrematchOddsSync:Enabled", false) &&
                   GetBoolean("SportMonksPrematchOddsSync:SyncFixtureOdds", true);
        }

        private bool ShouldSyncFixtureOddsSeed(string label)
        {
            // Defaults off so production has to flip the flag explicitly.
            // BACKLOG ONLY — not today. Alternative O/U lines (0.5/1.5/
            // 3.5/...) seed once at market open and don't update, so we
            // only need to capture them once per day. Running on every
            // pulse "today" tick (every 30 min) burned through the Odds
            // rate-limit bucket on 2026-05-21: 14 fixtures × ~5 paginated
            // pages = 70 calls × 2/hour = quota exhaustion + cascading
            // 429s. Backlog tier runs nightly, alongside predictions/
            // valuebets, when API quota is full.
            if (!GetBoolean("SportMonksPrematchOddsSync:Enabled", false) ||
                !GetBoolean("SportMonksPrematchOddsSync:SyncFixtureSeed", false))
                return false;

            return string.Equals(label, "backlog", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldSyncFixturePredictions(string label)
        {
            // Predictions are pulled per-fixture, so we only run them on the
            // pulse-tier "today" sweep (and backlog, for finished-match
            // probability snapshots) — not every 30s livescores tick.
            if (!GetBoolean("SportMonksPredictionsSync:Enabled", false) ||
                !GetBoolean("SportMonksPredictionsSync:SyncFixtureProbabilities", true))
                return false;

            return string.Equals(label, "today", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(label, "backlog", StringComparison.OrdinalIgnoreCase);
        }

        private bool ShouldSyncFixtureValueBets(string label)
        {
            // Same window/cadence as fixture probabilities (Track D1) —
            // value-bets ride the Odds & Predictions bundle and refresh on
            // the same schedule.
            if (!GetBoolean("SportMonksPredictionsSync:Enabled", false) ||
                !GetBoolean("SportMonksPredictionsSync:SyncValueBets", false))
                return false;

            return string.Equals(label, "today", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(label, "backlog", StringComparison.OrdinalIgnoreCase);
        }

        private async Task UpsertValueBetsForFixturesAsync(
            IReadOnlyList<Fixture> fixtures,
            CancellationToken cancellationToken)
        {
            // Same upcoming-or-recent window as predictions — value-bet
            // recommendations are pre-match by definition and meaningless
            // after kickoff.
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromDays(1);
            var targets = fixtures
                .Where(fixture => fixture != null && fixture.Id > 0)
                .Where(fixture =>
                {
                    if (fixture.StartingAt == null) return false;
                    var startsAt = new DateTimeOffset(fixture.StartingAt.Value, TimeSpan.Zero);
                    return startsAt > now - window;
                })
                .ToList();

            if (targets.Count == 0)
                return;

            foreach (var fixture in targets)
            {
                try
                {
                    var endpoint = $"predictions/value-bets/fixtures/{fixture.Id}";
                    var valueBets = (await _syncRunner.GetAllAsync<ValueBet>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.predictions.value-bets",
                            "analytics.sportmonks_value_bets",
                            "Sync SportMonks fixture value-bet recommendations."),
                        SportMonksApiRequest.Create(endpoint),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    if (valueBets.Count > 0)
                    {
                        await _valueBetsWriter.UpsertValueBetsForFixtureAsync(
                            fixture.Id,
                            valueBets,
                            cancellationToken);
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogWarning(
                        exc,
                        "Value-bets sync failed for fixture {FixtureId}: {Message}",
                        fixture.Id,
                        exc.Message);
                }
            }
        }

        private bool ShouldSyncFixtureMatchFacts(string label)
        {
            // Match facts are pre-match narrative stats (h2h streaks, form,
            // etc.) — they change slowly, so the today/backlog sweep is
            // enough; no need for the 30s livescores tick.
            if (!GetBoolean("SportMonksMatchFactsSync:Enabled", false) ||
                !GetBoolean("SportMonksMatchFactsSync:SyncFixtureMatchFacts", true))
                return false;

            return string.Equals(label, "today", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(label, "backlog", StringComparison.OrdinalIgnoreCase);
        }

        private async Task UpsertMatchFactsForFixturesAsync(
            IReadOnlyList<Fixture> fixtures,
            CancellationToken cancellationToken)
        {
            // Pre-match facts are most valuable while the fixture hasn't
            // kicked off yet (their primary use is the match-detail preview).
            // Skip anything older than a day so historical re-runs don't
            // hammer the endpoint for matches that are already done.
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromDays(1);
            var targets = fixtures
                .Where(fixture => fixture != null && fixture.Id > 0)
                .Where(fixture =>
                {
                    if (fixture.StartingAt == null) return false;
                    var startsAt = new DateTimeOffset(fixture.StartingAt.Value, TimeSpan.Zero);
                    return startsAt > now - window;
                })
                .ToList();

            if (targets.Count == 0)
                return;

            foreach (var fixture in targets)
            {
                try
                {
                    var endpoint = $"match-facts/{fixture.Id}";
                    var facts = (await _syncRunner.GetAllAsync<MatchFact>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.match-facts",
                            "football.fixture_match_facts",
                            "Sync SportMonks match facts (BETA) per fixture."),
                        SportMonksApiRequest.Create(endpoint),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    if (facts.Count > 0)
                    {
                        await _matchFactsWriter.UpsertMatchFactsForFixtureAsync(
                            fixture.Id,
                            facts,
                            cancellationToken);
                    }
                }
                catch (Exception exc)
                {
                    _logger.LogWarning(
                        exc,
                        "Match facts sync failed for fixture {FixtureId}: {Message}",
                        fixture.Id,
                        exc.Message);
                }
            }
        }

        private async Task UpsertPredictionsForFixturesAsync(
            IReadOnlyList<Fixture> fixtures,
            CancellationToken cancellationToken)
        {
            // Probabilities are stable until kickoff and refresh slowly after,
            // so an upcoming-or-recent filter keeps the per-fixture call count
            // predictable. Fixtures more than a day old are skipped — their
            // probability rows are already settled in our DB.
            var now = DateTimeOffset.UtcNow;
            var window = TimeSpan.FromDays(1);
            var targets = fixtures
                .Where(fixture => fixture != null && fixture.Id > 0)
                .Where(fixture =>
                {
                    if (fixture.StartingAt == null) return false;
                    var startsAt = new DateTimeOffset(fixture.StartingAt.Value, TimeSpan.Zero);
                    return startsAt > now - window;
                })
                .ToList();

            if (targets.Count == 0)
                return;

            foreach (var fixture in targets)
            {
                try
                {
                    var endpoint = $"predictions/probabilities/fixtures/{fixture.Id}";
                    var predictions = (await _syncRunner.GetAllAsync<PreMatchPrediction>(
                        SportMonksSyncJobDefinition.Create(
                            "sportmonks.football.predictions.probabilities",
                            "analytics.sportmonks_predictions",
                            "Sync SportMonks fixture probability predictions."),
                        SportMonksApiRequest.Create(endpoint),
                        cursorKey: endpoint,
                        cancellationToken: cancellationToken)).ToList();

                    if (predictions.Count > 0)
                    {
                        await _predictionsWriter.UpsertPredictionsForFixtureAsync(
                            fixture.Id,
                            predictions,
                            cancellationToken);
                    }
                }
                catch (Exception exc)
                {
                    // Predictions can 404 on unsupported fixtures (insufficient
                    // data) — log and keep going so a single bad fixture doesn't
                    // tank the whole pulse tick.
                    _logger.LogWarning(
                        exc,
                        "Predictions sync failed for fixture {FixtureId}: {Message}",
                        fixture.Id,
                        exc.Message);
                }
            }
        }

        private async Task UpsertPrematchOddsSeedForFixturesAsync(
            IReadOnlyList<Fixture> fixtures,
            CancellationToken cancellationToken)
        {
            // ALT-LINE INGEST (2026-05-21 redesign).
            //
            // Goal: pull every total/handicap line for a configurable set of
            // markets (default: market 80 / Total Goals O/U) per upcoming
            // fixture, so the mobile fixture detail screen shows 0.5/1.5/
            // 2.5/.../5.5 instead of just the 2.5 main line.
            //
            // Endpoint shape (verified via Postman):
            //   GET /v3/football/odds/pre-match
            //     ?filters=bookmakers:N;fixtures:M;markets:K
            //     &per_page=50
            //   Returns up to ~25 rows for a single (bookmaker, fixture,
            //   market) tuple. SportMonks caps per_page at 50 — that fits
            //   in a single page, so we BYPASS SyncRunner's pagination loop
            //   entirely (it was looping forever earlier today because
            //   the listing endpoint reports has_more=true even when the
            //   single page already contains every row).
            //
            // We use _apiClient.GetAsync directly to read exactly one page.
            // One call per (fixture, market). No SyncRunner pagination, no
            // raw_payloads archival, no cursor row — those add overhead
            // that's pointless for an idempotent line-tree refresh.
            var now = DateTimeOffset.UtcNow;
            var lookback = TimeSpan.FromHours(2);
            var lookahead = TimeSpan.FromHours(
                GetInteger("SportMonksPrematchOddsSync:SeedLookaheadHours", 48));

            var targets = fixtures
                .Where(fixture => fixture != null && fixture.Id > 0)
                .Where(fixture =>
                {
                    if (fixture.StartingAt == null) return false;
                    var startsAt = new DateTimeOffset(fixture.StartingAt.Value, TimeSpan.Zero);
                    return startsAt > now - lookback && startsAt < now + lookahead;
                })
                .ToList();

            if (targets.Count == 0)
                return;

            // SportMonks rejects `filters=fixtures:N` standing alone with
            // 400 ("You requested filters do not exist"). Pair fixtures
            // filter with the bookmaker allowlist (writer drops anything
            // outside that list anyway) — also scopes the response to
            // the bookmaker we care about, smaller payload, no waste.
            var allowedBookmakers = _configuration
                .GetSection("SportMonksPrematchOddsSync:AllowedBookmakerIds")
                .Get<long[]>() ?? Array.Empty<long>();
            if (allowedBookmakers.Length == 0)
            {
                _logger.LogWarning(
                    "Alt-line seed skipped: SportMonksPrematchOddsSync:AllowedBookmakerIds is empty " +
                    "(SportMonks rejects `filters=fixtures:N` without a companion filter).");
                return;
            }
            var bookmakersFilter = string.Join(
                ",",
                allowedBookmakers.Select(id => id.ToString(CultureInfo.InvariantCulture)));

            // Comma-separated list of market IDs to seed. Default 80 only
            // (Total Goals O/U). Expand to "80,28" once we want Asian
            // Handicap full handicap tree too. Each market_id is sent in
            // its OWN call so per_page=50 always fits the response in
            // one page (single market × ~25 rows max).
            var marketsCsv = NullIfWhiteSpace(
                _configuration["SportMonksPrematchOddsSync:SeedMarketIds"]) ?? "80";
            var marketIds = marketsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            // Pace the loop so we don't burst the Odds rate-limit bucket.
            // 500ms × ~50 fixtures × ~1 market ≈ 25s — fits comfortably
            // inside the nightly backlog window without starving the
            // /odds/pre-match/latest pulse tier.
            var throttle = TimeSpan.FromMilliseconds(
                GetInteger("SportMonksPrematchOddsSync:SeedDelayMs", 500));

            var totalUpserted = 0;
            var first = true;
            foreach (var fixture in targets)
            {
                foreach (var marketIdStr in marketIds)
                {
                    if (!first && throttle > TimeSpan.Zero)
                        await Task.Delay(throttle, cancellationToken);
                    first = false;

                    try
                    {
                        var request = SportMonksApiRequest.Create("odds/pre-match")
                            .WithFilters(
                                $"bookmakers:{bookmakersFilter}",
                                $"fixtures:{fixture.Id.ToString(CultureInfo.InvariantCulture)}",
                                $"markets:{marketIdStr}")
                            .WithQueryParameter("per_page", "50");

                        // Direct single-page read — NO pagination loop.
                        // SportMonks's has_more is unreliable on this
                        // endpoint shape and would otherwise spin until
                        // cancellation.
                        var response = await _apiClient.GetAsync<List<PreMatchOdd>>(
                            request, cancellationToken);
                        var odds = response.Data ?? new List<PreMatchOdd>();

                        if (odds.Count > 0)
                        {
                            await _prematchOddsWriter.UpsertPrematchOddsForFixtureAsync(
                                fixture.Id, odds, cancellationToken);
                            totalUpserted += odds.Count;
                        }
                    }
                    catch (SportMonksApiException ex)
                        when ((int)ex.StatusCode == 403 || (int)ex.StatusCode == 404)
                    {
                        // 403/404 = subscription doesn't cover this market for
                        // this fixture, or no odds yet. Non-fatal.
                        _logger.LogDebug(
                            "Alt-line seed {Status} for fixture {FixtureId} market {MarketId} — skipping.",
                            (int)ex.StatusCode, fixture.Id, marketIdStr);
                    }
                    catch (SportMonksApiException ex)
                        when ((int)ex.StatusCode == 429)
                    {
                        // Rate limit hit — back off and abort the seed pass.
                        // The Odds bucket resets within ~10-60 min; better
                        // to leave alternative lines stale for one cycle
                        // than to spam 429s and grow the back-off queue.
                        _logger.LogWarning(
                            "Alt-line seed hit rate limit on fixture {FixtureId} market {MarketId} — aborting seed pass.",
                            fixture.Id, marketIdStr);
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Alt-line seed failed for fixture {FixtureId} market {MarketId}: {Message}",
                            fixture.Id, marketIdStr, ex.Message);
                    }
                }
            }

            if (totalUpserted > 0)
            {
                _logger.LogInformation(
                    "Alt-line seed upserted {RowCount} odds across {FixtureCount} fixtures × {MarketCount} markets.",
                    totalUpserted, targets.Count, marketIds.Count);
            }
        }

        private async Task MaybeRunLatestPrematchOddsAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksPrematchOddsSync:Enabled", false) ||
                !GetBoolean("SportMonksPrematchOddsSync:SyncLatestOdds", false))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:PrematchOddsIntervalSeconds", 300);
            if (!_scheduler.ShouldRun(ScheduleKey.PrematchOdds, interval))
                return;

            try
            {
                var odds = (await _syncRunner.GetAllAsync<PreMatchOdd>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.odds.prematch.latest",
                        "odds.prematch_odds_current",
                        "Sync latest updated SportMonks pre-match odds."),
                    SportMonksApiRequest.Create("odds/pre-match/latest"),
                    cancellationToken: cancellationToken)).ToList();

                if (odds.Count > 0)
                {
                    var byFixture = odds
                        .Where(o => o.FixtureId > 0)
                        .GroupBy(o => o.FixtureId);

                    foreach (var group in byFixture)
                    {
                        try
                        {
                            await _prematchOddsWriter.UpsertPrematchOddsForFixtureAsync(
                                group.Key, group, cancellationToken);
                        }
                        catch (Exception exc)
                        {
                            // /odds/pre-match/latest returns odds for every
                            // fixture in our subscription window, including
                            // ones our fixtures table hasn't seen yet (FK
                            // violation 23503). Skip them — the next backlog
                            // tick brings the fixture row in, then the next
                            // pulse picks the odds back up.
                            _logger.LogWarning(
                                exc,
                                "Skipped prematch-odds upsert for fixture {FixtureId}: {Message}",
                                group.Key,
                                exc.Message);
                        }
                    }
                }
            }
            catch (SportMonksApiException ex)
                when ((int)ex.StatusCode == 403 || (int)ex.StatusCode == 404)
            {
                // odds/latest gates on the Odds & Predictions add-on; without
                // it SportMonks returns 403/404. The fixture-scoped odds path
                // (via includes) still covers what we have access to.
                _logger.LogWarning(
                    "odds/latest returned {Status}; subscription doesn't include the Odds & Predictions add-on.",
                    (int)ex.StatusCode);
            }

            _scheduler.RecordRun(ScheduleKey.PrematchOdds);
        }

        private async Task MaybeRunLatestInplayOddsAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("SportMonksInplayOddsSync:Enabled", false) ||
                !GetBoolean("SportMonksInplayOddsSync:SyncLatestOdds", true))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:InplayOddsIntervalSeconds", 30);
            if (!_scheduler.ShouldRun(ScheduleKey.InplayOdds, interval))
                return;

            var requestDelayMs = GetInteger("SportMonksInplayOddsSync:RequestDelayMs", 1000);

            try
            {
                var odds = (await _syncRunner.GetAllAsync<InplayOdd>(
                    SportMonksSyncJobDefinition.Create(
                        "sportmonks.football.odds.inplay.latest",
                        "odds.inplay_odds_current",
                        "Sync latest updated SportMonks inplay odds."),
                    SportMonksApiRequest.Create("odds/inplay/latest")
                        .WithRequestDelayMs(requestDelayMs),
                    cancellationToken: cancellationToken)).ToList();

                if (odds.Count > 0)
                {
                    var byFixture = odds
                        .Where(o => o.FixtureId > 0)
                        .GroupBy(o => o.FixtureId);

                    foreach (var group in byFixture)
                    {
                        try
                        {
                            await _inplayOddsWriter.UpsertInplayOddsForFixtureAsync(
                                group.Key, group, cancellationToken);
                        }
                        catch (Exception exc)
                        {
                            _logger.LogWarning(
                                exc,
                                "Skipped inplay-odds upsert for fixture {FixtureId}: {Message}",
                                group.Key,
                                exc.Message);
                        }
                    }
                }
            }
            catch (SportMonksApiException ex)
                when ((int)ex.StatusCode == 403 || (int)ex.StatusCode == 404)
            {
                _logger.LogWarning(
                    "odds/inplay/latest returned {Status}; subscription doesn't include the Odds & Predictions add-on.",
                    (int)ex.StatusCode);
            }

            _scheduler.RecordRun(ScheduleKey.InplayOdds);
        }

        private async Task MaybeRunAnalyticsAsync(CancellationToken cancellationToken)
        {
            if (!GetBoolean("AnalyticsSync:Enabled", false))
                return;

            var interval = GetInteger("SportMonksWorkerSettings:AnalyticsIntervalSeconds", 3600);
            if (!_scheduler.ShouldRun(ScheduleKey.Analytics, interval))
                return;

            if (GetBoolean("AnalyticsSync:RunSeasonStats", true))
                await _analyticsEngine.RunSeasonStatsAsync(cancellationToken);

            if (GetBoolean("AnalyticsSync:RunSeasonTeamStats", true))
                await _analyticsEngine.RunSeasonTeamStatsAsync(cancellationToken);

            if (GetBoolean("AnalyticsSync:RunSeasonPlayerStats", true))
                await _analyticsEngine.RunSeasonPlayerStatsAsync(cancellationToken);

            if (GetBoolean("AnalyticsSync:RunOddAnalysisSnapshots", true))
                await _analyticsEngine.RunOddAnalysisSnapshotsAsync(cancellationToken);

            if (GetBoolean("AnalyticsSync:RunFixtureSignals", true))
                await _analyticsEngine.RunFixtureSignalsAsync(cancellationToken);

            _scheduler.RecordRun(ScheduleKey.Analytics);
        }

        /// <summary>
        /// Hard-removes soft-deleted accounts whose audit row is older
        /// than the configured retention window. Runs once a day; the
        /// retention window defaults to 30 days (Apple/Google's standard
        /// "this should actually disappear" expectation).
        /// </summary>
        private async Task MaybeRunAccountPurgeAsync(CancellationToken cancellationToken)
        {
            var interval = GetInteger(
                "AccountPurge:IntervalSeconds", 86400);
            if (!_scheduler.ShouldRun(ScheduleKey.AccountPurge, interval))
                return;

            var retentionDays = GetInteger("AccountPurge:RetentionDays", 30);
            try
            {
                await _accountPurge.PurgeStaleAccountsAsync(
                    retentionDays, cancellationToken);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(
                    ex, "Account purge job failed; will retry on the next tick.");
            }

            _scheduler.RecordRun(ScheduleKey.AccountPurge);
        }

        private string GetFixtureByDateRangeEndpoint()
        {
            return NullIfWhiteSpace(_configuration["SportMonksUrls:fixtureByDateRange"]) ?? "fixtures/between/";
        }

        private string GetConfiguredEndpoint(string key, string defaultValue)
        {
            return NullIfWhiteSpace(_configuration[key]) ?? defaultValue;
        }

        private bool GetBoolean(string key, bool defaultValue)
        {
            var value = _configuration[key];

            if (bool.TryParse(value, out var result))
                return result;

            if (value == "1") return true;
            if (value == "0") return false;

            return defaultValue;
        }

        private int GetInteger(string key, int defaultValue)
        {
            return int.TryParse(
                _configuration[key],
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var result)
                ? result
                : defaultValue;
        }

        private static string? NullIfWhiteSpace(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Football worker started.");
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Football worker stopped.");
            return base.StopAsync(cancellationToken);
        }
    }
}
