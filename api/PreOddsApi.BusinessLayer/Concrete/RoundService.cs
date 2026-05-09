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
    public class RoundService : IRoundService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public RoundService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public RoundBusinessModel GetRound(long roundId)
        {
            return _mapper.Map<RoundBusinessModel>(_unitOfWork.Repository<round>().Get(p => p.id == roundId));
        }

        public List<RoundBusinessModel> GetRounds(long stageId)
        {
            return _mapper.Map<List<RoundBusinessModel>>(_unitOfWork.Repository<round>().GetList(p => p.stageId == stageId)).OrderByDescending(p=>p.Id).ToList();
        }
    }
}
