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
    public class VenueService : IVenueService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public VenueService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public VenueBusinessModel GetVenue(long venueId)
        {
            var venue = _unitOfWork.Repository<venue>().Get(p => p.id == venueId);
            if (venue != null)
            {
                return _mapper.Map<VenueBusinessModel>(venue);
            }
            else
            {
                return new VenueBusinessModel();
            }
        }
    }
}
