using AutoMapper;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football;
using PreOddsApi.Entities.SportMonks.Football.Standings.V3;
using PreOddsApi.Entities.SportMonks.Football.Statistics.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace SportMonks.Football.FixtureWorker.Mapping
{
    public class FootballMapping : Profile
    {
        public FootballMapping()
        {
            CreateMap<Sport, sport>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();
            CreateMap<Round, round>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Stage, stage>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Season, season>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Group, group>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Lineup, lineup>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Fixture, fixture>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .AfterMap((s, d) => d.localTeamHtScore = s.Scores == null ? 0 : s.Scores.FirstOrDefault(x => x.TypeId == 1 && x.ParticipantId == s.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Home).Id).Goal.Goals)
                .AfterMap((s, d) => d.visitorTeamHtScore = s.Scores == null ? 0 : s.Scores.FirstOrDefault(x => x.TypeId == 1 && x.ParticipantId == s.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Away).Id).Goal.Goals)
                .AfterMap((s, d) => d.localTeamFtScore = s.Scores == null ? 0 : s.Scores.FirstOrDefault(x => x.TypeId == 2 && x.ParticipantId == s.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Home).Id).Goal.Goals)
                .AfterMap((s, d) => d.visitorTeamFtScore = s.Scores == null ? 0 : s.Scores.FirstOrDefault(x => x.TypeId == 2 && x.ParticipantId == s.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Away).Id).Goal.Goals)
                .AfterMap((s, d) => d.localTeamId = s.Participants == null ? 0 : s.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Home).Id)
                .AfterMap((s, d) => d.visitorTeamId = s.Participants == null ? 0 : s.Participants.FirstOrDefault(x => x.Meta.Location == PreOddsApi.Entities.SportMonks.Common.Enums.Location.Away).Id)
                .AfterMap((s, d) => d.status = s.State.DeveloperName)
                .ReverseMap();


            CreateMap<Fixture, prd_fixture_of_day>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<State, state>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<League, league>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Team, team>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Participant, team>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<PreMatchOdd, odd>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Event, events>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .AfterMap((s, d) => d.teamId = s.ParticipantId)
                .ReverseMap();

            CreateMap<Statistic, statistic>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Player, player>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Standing, standing>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .AfterMap((s, d) => d.teamId = s.ParticipantId)
                .ReverseMap();

            CreateMap<StandingRule, standing_rule>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<StandingDetail, standing_detail>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<StandingForm, standing_form>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();
                
            CreateMap<TvStation, tvstation>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .ReverseMap();

            CreateMap<Sidelined, sidelined>().AfterMap((s, d) => d.sportmonks_id = s.Id)
                .AfterMap((s, d) => d.teamId = s.Sideline.TeamId)
                .AfterMap((s, d) => d.typeId = s.Sideline.TypeId)
                .AfterMap((s, d) => d.playerId = s.Sideline.PlayerId)
                .AfterMap((s, d) => d.seasonId = s.Sideline.SeasonId)
                .AfterMap((s, d) => d.category = s.Sideline.Category)
                .AfterMap((s, d) => d.completed = s.Sideline.Completed)
                .AfterMap((s, d) => d.startDate = s.Sideline.StartDate)
                .AfterMap((s, d) => d.endDate = s.Sideline.EndDate)
                .AfterMap((s, d) => d.gamesMissed = s.Sideline.GamesMissed)
                .ReverseMap();
        }
    }
}
