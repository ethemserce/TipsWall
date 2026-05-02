using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class TopScorerService : ITopScorerService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly ITeamService _teamService;
        private readonly IMapper _mapper;

        public TopScorerService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IPlayerService playerService, ITeamService teamService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _teamService = teamService;
            _mapper = mapper;
        }

        public TopScorerBusinessModel GetTopScorers(long leagueId, long seasonId, long stageId, string lang)
        {
            TopScorerBusinessModel topScorers = new TopScorerBusinessModel();
            topScorers.AssistsScorer = _mapper.Map<List<AssistsscorerBusinessModel>>(_unitOfWork.Repository<assistscorer>().GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId).OrderBy(p => p.position).Take(10));
            topScorers.CardScorer = _mapper.Map<List<CardscorerBusinessModel>>(_unitOfWork.Repository<cardscorer>().GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId).OrderBy(p => p.position).Take(10));
            topScorers.GoalScorer = _mapper.Map<List<GoalscorerBusinessModel>>(_unitOfWork.Repository<goalscorer>().GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId).OrderBy(p => p.position).Take(10));

            foreach (var assists in topScorers.AssistsScorer)
            {
                assists.Player = _playerService.GetPlayer(assists.PlayerId);
                assists.Team = _teamService.GetTeam(assists.TeamId);
            }

            foreach (var cardscorer in topScorers.CardScorer)
            {
                cardscorer.Player = _playerService.GetPlayer(cardscorer.PlayerId);
                cardscorer.Team = _teamService.GetTeam(cardscorer.TeamId);
            }

            foreach (var goalscorer in topScorers.GoalScorer)
            {
                goalscorer.Player = _playerService.GetPlayer(goalscorer.PlayerId);
                goalscorer.Team = _teamService.GetTeam(goalscorer.TeamId);
            }
            return topScorers;
        }      
    }
}
