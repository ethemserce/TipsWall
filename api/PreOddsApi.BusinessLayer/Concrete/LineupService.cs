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
    public class LineupService : ILineupService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly IMapper _mapper;

        public LineupService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IPlayerService playerService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _mapper = mapper;
        }

        public List<LineupBusinessModel> GetLineups(long fixtureId, long teamId)
        {
            //return _mapper.Map<List<LineupBusinessModel>>(_unitOfWork.Repository<lineup>().GetList(p => p.fixtureId == fixtureId && p.team_id == teamId));
            var lineups = _mapper.Map<List<LineupBusinessModel>>(_unitOfWork.Repository<lineup>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId)).ToList();
            if (lineups != null)
            {
            
                if (lineups.Count() > 0)
                {
                    foreach (var item in lineups)
                    {
                        item.Player = _playerService.GetPlayer(item.PlayerId);
                    }
                    return lineups;
                }
            }

            return new List<LineupBusinessModel>();
        }
    }
}
