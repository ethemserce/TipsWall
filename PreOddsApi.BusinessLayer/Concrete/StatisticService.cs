using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System.Collections.Generic;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class StatisticService : IStatisticService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly ITeamService _teamService;
        private readonly IPlayerService _playerService;
        private readonly IMapper _mapper;

        public StatisticService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, ITeamService teamService, IPlayerService playerService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _teamService = teamService;
            _playerService = playerService;
            _mapper = mapper;
        }

        public List<StatisticBusinessModel> GetStatistics(long fixtureId, long teamId)
        {
            var statisticList = _mapper.Map<List<StatisticBusinessModel>>(_unitOfWork.Repository<statistic>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId)).ToList();
            if (statisticList != null)
            {
                var types = _unitOfWork.Repository<types>().GetList().ToList();
                foreach (var item in statisticList)
                {
                    var type = types.FirstOrDefault(x => x.id == item.TypeId);
                    item.Type = type.code;
                    item.TypeName = type.name;
                }
                return statisticList;
            }
            else
            {
                return new List<StatisticBusinessModel>();
            }
        }

        public List<StatisticBusinessModel> GetStatistics(long fixtureId)
        {
            var statistics = _unitOfWork.Repository<statistic>().GetList(p => p.fixtureId == fixtureId).ToList();

            if (statistics != null)
            {
                var statisticGroups = statistics.GroupBy(x => x.typeId).ToList();
                var types = _unitOfWork.Repository<types>().GetList(x=>x.modelType == "statistic").ToList();
                

                List<StatisticBusinessModel> statisticList = new List<StatisticBusinessModel>();
                foreach (var statisticGroupItem in statisticGroups)
                {
                    var statisticBusinessModel = new StatisticBusinessModel();
                    foreach (var item in statisticGroupItem)
                    {
                        var type = types.FirstOrDefault(x => x.id == item.typeId);
                        var team = _unitOfWork.Repository<team>().Get(x => x.id == item.teamId);

                        statisticBusinessModel.Type = type.code;
                        statisticBusinessModel.TypeName = type.name;
                        statisticBusinessModel.TypeId = type.id;
                        statisticBusinessModel.StatGroup = type.statGroup;
                        if (item.location == "home")
                        {
                            statisticBusinessModel.LocalTeamId = item.id;
                            statisticBusinessModel.LocalTeamValue = item.value;
                            statisticBusinessModel.LocalTeamName = team.name;
                        }
                        else if (item.location == "away")
                        {
                            statisticBusinessModel.VisitorTeamId = item.id;
                            statisticBusinessModel.VisitorTeamValue = item.value;
                            statisticBusinessModel.VisitorTeamName = team.name;
                        }
                    }
                    statisticList.Add(statisticBusinessModel);
                }

                return statisticList;
            }
            else
            {
                return new List<StatisticBusinessModel>();
            }
        }

        public SeasonStatsBusinessModel GetSeasonStats(long leagueId, long seasonId)
        {
            var seasonstats = _unitOfWork.Repository<seasonstats>().Get(p => p.leagueId == leagueId && p.seasonId == seasonId); //_mapper.Map<SeasonStatsBusinessModel>();

            SeasonStatsBusinessModel seasonStatsBusinessModel = null;
            if (seasonstats != null)
            {
                seasonStatsBusinessModel = new SeasonStatsBusinessModel()
                {
                    Id = seasonstats.id,
                    SeasonId = seasonstats.seasonId,
                    LeagueId = seasonstats.leagueId,
                    NumberOfClubs = seasonstats.number_of_clubs,
                    NumberOfMatches = seasonstats.number_of_matches,
                    NumberOfMatchesPlayed = seasonstats.number_of_matches_played,
                    NumberOfGoals = seasonstats.number_of_goals,
                    MatchesBothTeamsScored = seasonstats.matches_both_teams_scored,
                    NumberOfYellowcards = seasonstats.number_of_yellowcards,
                    NumberOfYellowredcards = seasonstats.number_of_yellowredcards,
                    NumberOfRedcards = seasonstats.number_of_redcards,
                    AvgGoalsPerMatch = seasonstats.avg_goals_per_match,
                    AvgYellowcardsPerMatch = seasonstats.avg_yellowcards_per_match,
                    AvgYellowredcardsPerMatch = seasonstats.avg_yellowredcards_per_match,
                    AvgRedcardsPerMatch = seasonstats.avg_redcards_per_match,
                    GoalScoredEveryMinutes = seasonstats.goal_scored_every_minutes,
                    GoalsScoredMinutes0 = seasonstats.goals_scored_minutes_0,
                    GoalsScoredMinutes15 = seasonstats.goals_scored_minutes_15,
                    GoalsScoredMinutes30 = seasonstats.goals_scored_minutes_30,
                    GoalsScoredMinutes45 = seasonstats.goals_scored_minutes_45,
                    GoalsScoredMinutes60 = seasonstats.goals_scored_minutes_60,
                    GoalsScoredMinutes75 = seasonstats.goals_scored_minutes_75,
                    CreateDateTime = seasonstats.create_date_time,
                    UpdateDateTime = seasonstats.update_date_time
                };

                if (seasonstats.team_with_most_goals_id != null)
                {
                    seasonStatsBusinessModel.TeamWithMostGoals = _teamService.GetTeam((long)seasonstats.team_with_most_goals_id);
                }

                if (seasonstats.team_with_most_conceded_goals_id != null)
                {
                    seasonStatsBusinessModel.TeamWithMostConcededGoals = _teamService.GetTeam((long)seasonstats.team_with_most_conceded_goals_id);
                }

                if (seasonstats.team_with_most_goals_per_match_id != null)
                {
                    seasonStatsBusinessModel.TeamWithMostGoalsPerMatch = _teamService.GetTeam((long)seasonstats.team_with_most_goals_per_match_id);
                }

                if (seasonstats.team_most_cleansheets_id != null)
                {
                    seasonStatsBusinessModel.TeamMostCleansheets = _teamService.GetTeam((long)seasonstats.team_most_cleansheets_id);
                }

                if (seasonstats.season_topscorer_id != null)
                {
                    seasonStatsBusinessModel.SeasonTopscorer = _playerService.GetPlayer((long)seasonstats.season_topscorer_id);
                }

                if (seasonstats.season_assist_topscorer_id != null)
                {
                    seasonStatsBusinessModel.SeasonAssistTopscorer = _playerService.GetPlayer((long)seasonstats.season_assist_topscorer_id);
                }

                if (seasonstats.goalkeeper_most_cleansheets_id != null)
                {
                    seasonStatsBusinessModel.GoalkeeperMostCleansheets = _playerService.GetPlayer((long)seasonstats.goalkeeper_most_cleansheets_id);
                }
            }

            return seasonStatsBusinessModel;
        }
    }
}
