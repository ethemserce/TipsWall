using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;
using System.Collections.Generic;
using System.Linq;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class StandingService : IStandingService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IGroupService _groupService;
        private readonly ITeamService _teamService;

        public StandingService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IGroupService groupService, ITeamService teamService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _groupService = groupService;
            _teamService = teamService;
            _mapper = mapper;
        }

        public List<StandingBusinessModel> GetStandings(long leagueId, long seasonId, long stageId, long groupId)
        {
            //GroupBusinessModel group = _groupService.GetGroup(groupId);
            if (groupId != 0)
            {
                var standings = _mapper.Map<List<StandingBusinessModel>>(_unitOfWork.Repository<standing>().GetList(p => p.leagueId == leagueId && p.seasonId== seasonId && p.stageId == stageId && p.groupId== groupId));
                foreach (var standing in standings)
                {
                    standing.Team = _teamService.GetTeam(standing.StandingsTeamId);
                }
                return standings.OrderBy(p => p.StandingsPositon).ToList();
            }
            else
            {
                var dbStandings = _unitOfWork.Repository<standing>().GetList(p => p.leagueId == leagueId && p.seasonId == seasonId && p.stageId == stageId);
                var standings = _mapper.Map<List<StandingBusinessModel>>(dbStandings);
                foreach (var standing in standings)
                {
                    standing.Team = _teamService.GetTeam(standing.StandingsTeamId);
                }
                return standings.OrderBy(p=>p.StandingsPositon).ToList();
            }
        }

        public StandingBusinessModel GetStanding(long teamId)
        {
            var standing = _mapper.Map<StandingBusinessModel>(_unitOfWork.Repository<standing>().Get(p => p.teamId == teamId));
            standing.Team = _teamService.GetTeam(standing.StandingsTeamId);
            return standing;
        }
    }
}
