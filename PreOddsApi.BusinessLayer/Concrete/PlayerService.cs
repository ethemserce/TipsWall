using AutoMapper;
using PreOddsApi.BusinessLayer.Abstract;
using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.BusinessLayer.Concrete
{
    public class PlayerService : IPlayerService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public PlayerService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public PlayerBusinessModel GetPlayer(long playerId)
        {
            var player = _unitOfWork.Repository<player>().Get(p => p.id == playerId);
            if (player != null)
            {
                return _mapper.Map<PlayerBusinessModel>(player);
            }
            else
            {
                return new PlayerBusinessModel();
            }

        }
    }
}
