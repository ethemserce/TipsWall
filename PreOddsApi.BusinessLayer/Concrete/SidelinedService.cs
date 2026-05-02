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
    public class SidelinedService : ISidelinedService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IPlayerService _playerService;
        private readonly IMapper _mapper;

        public SidelinedService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IPlayerService playerService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _playerService = playerService;
            _mapper = mapper;
        }

        public List<SidelinedBusinessModel> GetSidelineds(long fixtureId, long teamId)
        {
            //return _mapper.Map<List<SidelinedBusinessModel>>(_unitOfWork.Repository<sidelined>().GetList(p => p.fixtureId == fixtureId && p.team_id == teamId));
            var sidelined = _unitOfWork.Repository<sidelined>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId);
            if (sidelined != null)
            {
                if (sidelined.Count() > 0)
                {
                    var sidelineds = _mapper.Map<List<SidelinedBusinessModel>>(sidelined);
                    foreach (var item in sidelineds)
                    {
                        item.Player = _playerService.GetPlayer(item.PlayerId);
                    }
                    return sidelineds;
                }
            }

            return new List<SidelinedBusinessModel>();
        }
    }
}
