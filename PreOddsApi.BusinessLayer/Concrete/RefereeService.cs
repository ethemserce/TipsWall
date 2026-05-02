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
    public class RefereeService : IRefereeService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public RefereeService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public RefereeBusinessModel GetReferee(long refereeId)
        {
            var referee = _unitOfWork.Repository<referee>().Get(p => p.id == refereeId);
            if (referee != null)
            {
                return _mapper.Map<RefereeBusinessModel>(referee);
            }
            else
            {
                return new RefereeBusinessModel();
            }
        }
    }
}
