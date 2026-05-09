using Microsoft.EntityFrameworkCore;
using PreOddsApi.DataLayer.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer
{
    public class PreOddsApiDbContext : DbContext
    {
        public PreOddsApiDbContext(DbContextOptions<PreOddsApiDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new ContinentMap());
            modelBuilder.ApplyConfiguration(new CountryMap());
            modelBuilder.ApplyConfiguration(new CityMap());
            modelBuilder.ApplyConfiguration(new RegionMap());
            modelBuilder.ApplyConfiguration(new TypesMap());

            modelBuilder.ApplyConfiguration(new BenchMap());
            modelBuilder.ApplyConfiguration(new BookmakerMap());
            modelBuilder.ApplyConfiguration(new CoachMap());
            modelBuilder.ApplyConfiguration(new CommentMap());

            modelBuilder.ApplyConfiguration(new ContinentLocaleMap());
            modelBuilder.ApplyConfiguration(new CornerMap());

            modelBuilder.ApplyConfiguration(new EventsMap());
            modelBuilder.ApplyConfiguration(new CountryLocaleMap());
            modelBuilder.ApplyConfiguration(new FixtureMap());
            modelBuilder.ApplyConfiguration(new GroupMap());
            modelBuilder.ApplyConfiguration(new HighlightMap());
            modelBuilder.ApplyConfiguration(new LeagueMap());
            modelBuilder.ApplyConfiguration(new LineupMap());
            modelBuilder.ApplyConfiguration(new MarketMap());
            modelBuilder.ApplyConfiguration(new OddMap());
            modelBuilder.ApplyConfiguration(new PlayerMap());
            modelBuilder.ApplyConfiguration(new RefereeMap());
            modelBuilder.ApplyConfiguration(new RoundMap());
            modelBuilder.ApplyConfiguration(new SeasonMap());
            modelBuilder.ApplyConfiguration(new SidelinedMap());
            modelBuilder.ApplyConfiguration(new SportMap());
            modelBuilder.ApplyConfiguration(new StageMap());
            modelBuilder.ApplyConfiguration(new StandingMap());
            modelBuilder.ApplyConfiguration(new StatisticMap());
            modelBuilder.ApplyConfiguration(new TeamMap());
            modelBuilder.ApplyConfiguration(new TvstationMap());
            modelBuilder.ApplyConfiguration(new VenueMap());
            modelBuilder.ApplyConfiguration(new AssistscorerMap());
            modelBuilder.ApplyConfiguration(new GoalscorerMap());
            modelBuilder.ApplyConfiguration(new CardscorerMap());
            modelBuilder.ApplyConfiguration(new OddAnalysisMap());
            modelBuilder.ApplyConfiguration(new PrdUserMap());
            modelBuilder.ApplyConfiguration(new PrdCouponMap());
            modelBuilder.ApplyConfiguration(new PrdCouponItemMap());
            modelBuilder.ApplyConfiguration(new PrdTipsMap());
            modelBuilder.ApplyConfiguration(new PrdFixtureOfDayMap());
            modelBuilder.ApplyConfiguration(new SeasonstatsMap());
            modelBuilder.ApplyConfiguration(new SeasonstatsMap());


            modelBuilder.ApplyConfiguration(new CommentaryMap());
            modelBuilder.ApplyConfiguration(new RivalMap());
            modelBuilder.ApplyConfiguration(new SquadMap());
            modelBuilder.ApplyConfiguration(new ScheduleMap());
            modelBuilder.ApplyConfiguration(new StateMap());
            modelBuilder.ApplyConfiguration(new TopScorerMap());
            modelBuilder.ApplyConfiguration(new TransferMap());
            modelBuilder.ApplyConfiguration(new ScoreMap());

            modelBuilder.ApplyConfiguration(new StandingDetailMap());
            modelBuilder.ApplyConfiguration(new StandingRuleMap());
            modelBuilder.ApplyConfiguration(new StandingFormMap());
            modelBuilder.ApplyConfiguration(new AggregateMap());
            modelBuilder.ApplyConfiguration(new TrendMap());
            modelBuilder.ApplyConfiguration(new NewsMap());
            modelBuilder.ApplyConfiguration(new FormationMap());
            modelBuilder.ApplyConfiguration(new NewsItemLineMap());
            modelBuilder.ApplyConfiguration(new PeriodMap());
        }

        // Core
        public DbSet<continent> Continents { get; set; }
        public DbSet<country> Countries { get; set; }
        public DbSet<city> Cities { get; set; }
        public DbSet<region> Regions { get; set; }
        public DbSet<types> Types { get; set; }


        // Football
        public DbSet<bench> Benchs { get; set; }
        public DbSet<bookmaker> Bookmakers { get; set; }
        public DbSet<coach> Coaches { get; set; }
        public DbSet<comment> Comments { get; set; }
        public DbSet<aggregate> Aggregates { get; set; }
        public DbSet<continent_locale> ContinentLocales { get; set; }
        public DbSet<corner> Corners { get; set; }
        public DbSet<news> News { get; set; }
        public DbSet<newsItemLine> NewsItemLines { get; set; }
        public DbSet<events> Events { get; set; }
        public DbSet<country_locale> CountryLocales { get; set; }
        public DbSet<fixture> Fixtures { get; set; }
        public DbSet<group> Groups { get; set; }
        public DbSet<highlight> Highlights { get; set; }
        public DbSet<league> Leagues { get; set; }
        public DbSet<lineup> Lineups { get; set; }
        public DbSet<market> Markets { get; set; }
        public DbSet<odd> Odds { get; set; }
        public DbSet<player> Players { get; set; }
        public DbSet<referee> Referees { get; set; }
        public DbSet<round> Rounds { get; set; }
        public DbSet<season> Seasons { get; set; }
        public DbSet<sidelined> Sidelineds { get; set; }
        public DbSet<sport> Sports { get; set; }
        public DbSet<stage> Stages { get; set; }
        public DbSet<standing> Standings { get; set; }
        public DbSet<standing_detail> StandingDetails { get; set; }
        public DbSet<standing_rule> StandingRules { get; set; }
        public DbSet<standing_form> StandingForms { get; set; }
        public DbSet<statistic> Statistics { get; set; }
        public DbSet<team> Teams { get; set; }
        public DbSet<tvstation> Tvstations { get; set; }
        public DbSet<venue> Venues { get; set; }
        public DbSet<assistscorer> Assistscorers { get; set; }
        public DbSet<goalscorer> Goalscorers { get; set; }
        public DbSet<cardscorer> Cardscorers { get; set; }
        public DbSet<odd_analysis> OddAnalysis { get; set; }
        public DbSet<prd_user> PrdUsers { get; set; }
        public DbSet<prd_coupon> PrdCoupons { get; set; }
        public DbSet<prd_coupon_item> PrdCouponItems { get; set; }
        public DbSet<prd_tips> PrdTips { get; set; }
        public DbSet<prd_fixture_of_day> PrdFixtureOfDay { get; set; }
        public DbSet<seasonstats> Seasonstats { get; set; }
        public DbSet<commentary> Commentaries { get; set; }
        public DbSet<rival> Rivals { get; set; }
        public DbSet<squad> Squads { get; set; }
        public DbSet<schedule> Schedules { get; set; }
        public DbSet<state> States { get; set; }
        public DbSet<topScorer> TopScorers { get; set; }
        public DbSet<transfer> Transfers { get; set; }
        public DbSet<score> Scores { get; set; }
        public DbSet<trend> Trends { get; set; }
        public DbSet<formation> Formations { get; set; }
        public DbSet<period> Periods { get; set; }
    }
}
