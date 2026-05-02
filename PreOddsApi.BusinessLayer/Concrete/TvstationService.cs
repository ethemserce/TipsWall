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
    public class TvstationService : ITvstationService
    {

        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public TvstationService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public List<TvstationBusinessModel> GetTvstations(long fixtureId)
        {
            var tvstationList = _unitOfWork.Repository<tvstation>().GetList(p => p.fixtureId == fixtureId);
            if (tvstationList != null)
            {
                return _mapper.Map<List<TvstationBusinessModel>>(tvstationList);
            }
            else
            {
                return new List<TvstationBusinessModel>();
            }
        }
    }
}
