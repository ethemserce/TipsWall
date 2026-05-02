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
    public class CoachService : ICoachService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public CoachService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public CoachBusinessModel GetCoach(long coachId)
        {
            var coach = _unitOfWork.Repository<coach>().Get(p => p.id == coachId);
            if (coach != null)
            {
                return _mapper.Map<CoachBusinessModel>(coach);
            }
            else
            {
                return new CoachBusinessModel();
            }
        }
    }
}
