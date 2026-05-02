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
    public class CornerService: ICornerService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public CornerService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public List<CornerBusinessModel> GetCorners(long fixtureId, long teamId)
        {
            var cornerList = _unitOfWork.Repository<corner>().GetList(p => p.fixtureId == fixtureId && p.teamId == teamId);
            if (cornerList != null)
            {
                return _mapper.Map<List<CornerBusinessModel>>(cornerList);
            }
            else
            {
                return new List<CornerBusinessModel>();
            }
        }
    }
}
