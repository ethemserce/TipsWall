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
    public class StageService : IStageService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public StageService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public StageBusinessModel GetStage(long stageId)
        {
            return _mapper.Map<StageBusinessModel>(_unitOfWork.Repository<stage>().Get(p => p.id == stageId));
        }

        public List<StageBusinessModel> GetStages(long seasonId)
        {
            return _mapper.Map<List<StageBusinessModel>>(_unitOfWork.Repository<stage>().GetList(p => p.seasonId== seasonId)).OrderByDescending(p => p.Id).ToList();
        }
    }
}
