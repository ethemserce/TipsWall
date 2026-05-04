using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PreOddsApi.ExternalApis.SportMonks;
using PreOddsApi.ExternalApis.SportMonks.Sync;
using PreOddsApi.ExternalApis.SportMonks.Sync.Writers;

namespace PreOddsApi.ExternalApis.DependencyInjection
{
    public static class SportMonksApiServiceCollectionExtensions
    {
        public static IServiceCollection AddSportMonksApiClient(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var options = SportMonksApiOptions.FromConfiguration(configuration);

            services.AddSingleton(options);
            services.AddSingleton<ISportMonksSyncRunner, SportMonksSyncRunner>();
            services.AddSingleton<ISportMonksCatalogReferenceWriter, SportMonksCatalogReferenceWriter>();
            services.AddSingleton<ISportMonksCompetitionReferenceWriter, SportMonksCompetitionReferenceWriter>();
            services.AddSingleton<ISportMonksFootballCoreReferenceWriter, SportMonksFootballCoreReferenceWriter>();
            services.AddSingleton<ISportMonksFixtureCoreWriter, SportMonksFixtureCoreWriter>();
            services.AddSingleton<ISportMonksFixtureEventStatisticWriter, SportMonksFixtureEventStatisticWriter>();
            services.AddSingleton<ISportMonksFixtureLineupFormationWriter, SportMonksFixtureLineupFormationWriter>();
            services.AddSingleton<ISportMonksFixtureRefereeWriter, SportMonksFixtureRefereeWriter>();
            services.AddSingleton<ISportMonksOddsReferenceWriter, SportMonksOddsReferenceWriter>();
            services.AddSingleton<ISportMonksPlayerCoachSquadRivalWriter, SportMonksPlayerCoachSquadRivalWriter>();
            services.AddSingleton<ISportMonksStandingTopScorerWriter, SportMonksStandingTopScorerWriter>();
            services.AddSingleton<ISportMonksTransferSidelinedWriter, SportMonksTransferSidelinedWriter>();
            services.AddSingleton<ISportMonksFixtureMediaWeatherWriter, SportMonksFixtureMediaWeatherWriter>();
            services.AddSingleton<ISportMonksFixtureTrendCommentaryWriter, SportMonksFixtureTrendCommentaryWriter>();
            services.AddHttpClient<ISportMonksApiClient, SportMonksApiClient>(httpClient =>
            {
                httpClient.BaseAddress = new Uri(options.BaseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

            return services;
        }
    }
}
