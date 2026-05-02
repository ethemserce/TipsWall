using PreOddsApi.DataLayer;
using Microsoft.Extensions.DependencyInjection;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.Core.Data.EntityFramework.Concrete;
using Microsoft.Extensions.Configuration;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Concrete;
using PreOddsApi.Utils;
using Microsoft.EntityFrameworkCore;

namespace PreOddsApi.BusinessLayer.DependencyInjection
{
    public class DependencyService
    {
        public static void SetDependencyTypes(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var constr = configuration.GetConnectionString("PreOddsApiMySqlDb");

            serviceCollection.AddDbContext<PreOddsApiDbContext>(options =>
                options.UseMySql(constr,
                     ServerVersion.AutoDetect(constr)));

            //DataLayer
            serviceCollection.AddTransient<IUnitOfWork<PreOddsApiDbContext>, UnitOfWork<PreOddsApiDbContext>>();
            serviceCollection.AddTransient<IAnalysisUnitOfWork<PreOddsApiDbContext>, AnalysisUnitOfWork<PreOddsApiDbContext>>();
            serviceCollection.AddTransient<IBenchService, BenchService>();
            serviceCollection.AddTransient<ICoachService, CoachService>();
            serviceCollection.AddTransient<ICommentService, CommentService>();
            serviceCollection.AddTransient<IContinentService, ContinentService>();
            serviceCollection.AddTransient<ICountryService, CountryService>();
            serviceCollection.AddTransient<ICornerService, CornerService>();
            serviceCollection.AddTransient<IEventsService, EventsService>();
            serviceCollection.AddTransient<IFixtureService, FixtureService>();
            serviceCollection.AddTransient<IGroupService, GroupService>();
            serviceCollection.AddTransient<IHighlightService, HighlightService>();
            serviceCollection.AddTransient<ILeagueService, LeagueService>();
            serviceCollection.AddTransient<ILineupService, LineupService>();
            serviceCollection.AddTransient<IPlayerService, PlayerService>();
            serviceCollection.AddTransient<IRefereeService, RefereeService>();
            serviceCollection.AddTransient<IRoundService, RoundService>();
            serviceCollection.AddTransient<ISeasonService, SeasonService>();
            serviceCollection.AddTransient<ISidelinedService, SidelinedService>();
            serviceCollection.AddTransient<IStageService, StageService>();
            serviceCollection.AddTransient<IStandingService, StandingService>();
            serviceCollection.AddTransient<IStatisticService, StatisticService>();
            serviceCollection.AddTransient<ITeamService, TeamService>();
            serviceCollection.AddTransient<ITvstationService, TvstationService>();
            serviceCollection.AddTransient<IVenueService, VenueService>();

            serviceCollection.AddTransient<IMarketService, MarketService>();
            serviceCollection.AddTransient<IBookmakerService, BookmakerService>();
            serviceCollection.AddTransient<IOddService, OddService>();
            serviceCollection.AddTransient<ITipsService, TipsService>();
            serviceCollection.AddTransient<ITopScorerService, TopScorerService>();

            serviceCollection.AddTransient<IContactService, ContactService>();
            serviceCollection.AddTransient<IEMailHelper, PrdEMailHelper>();

            serviceCollection.AddTransient<IPrdUserService, PrdUserService>();
            serviceCollection.AddTransient<ICouponService, CouponService>();
            serviceCollection.AddTransient<IFixtureOfDayService, FixtureOfDayService>();

            serviceCollection.AddTransient<ICacheHelper, CacheHelper>();
            //serviceCollection.AddTransient<IBrowseLogService, BrowseLogService>(ctx =>
            //{
            //    var context =
            //        new WebsiteDbContext(
            //            ctx.GetRequiredService<DbContextOptions<WebsiteDbContext>>());
            //    var unitOfWork = new UnitOfWork<WebsiteDbContext>(context);

            //    return new BrowseLogService(unitOfWork);
            //});

            //serviceCollection.AddTransient<IExceptionLogService, ExceptionLogService>(ctx =>
            //{
            //    var context =
            //        new WebsiteDbContext(
            //            ctx.GetRequiredService<DbContextOptions<WebsiteDbContext>>());
            //    var unitOfWork = new UnitOfWork<WebsiteDbContext>(context);

            //    return new ExceptionLogService(unitOfWork);
            //});
        }
    }
}
