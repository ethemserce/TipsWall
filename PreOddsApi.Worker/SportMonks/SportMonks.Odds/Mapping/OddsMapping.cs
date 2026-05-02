using AutoMapper;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Odds.V3;

namespace SportMonks.Football.FixtureWorker.Mapping
{
    public class OddsMapping : Profile
    {
        public OddsMapping()
        {
            CreateMap<Market, market>()
                .AfterMap((s, d) => d.sportmonks_id = s.Id)
                .AfterMap((s, d) => d.legacy_id = s.LegacyId)
                .AfterMap((s, d) => d.has_winning_calculations = s.HasWinningCalculations)
                .AfterMap((s, d) => d.developer_name = s.DeveloperName)
                .AfterMap((s, d) => d.flag = 1)
                .ReverseMap();

            CreateMap<Bookmaker, bookmaker>()
                .AfterMap((s, d) => d.sportmonks_id = s.Id)
                .AfterMap((s, d) => d.legacy_id = s.LegacyId)
                .ReverseMap();

        }
    }
}
