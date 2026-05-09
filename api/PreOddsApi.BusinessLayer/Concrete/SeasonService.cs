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
    public class SeasonService : ISeasonService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public SeasonService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public SeasonBusinessModel GetSeason(long seasonId)
        {
            return _mapper.Map<SeasonBusinessModel>(_unitOfWork.Repository<season>().Get(p => p.id == seasonId));
        }

        public SeasonBusinessModel GetCurrentSeason(long leagueId)
        {
            return _mapper.Map<SeasonBusinessModel>(_unitOfWork.Repository<season>().Get(p => p.leagueId == leagueId && p.isCurrent == true));
        }

        public List<SeasonBusinessModel> GetSeasons(long leagueId)
        {
            return _mapper.Map<List<SeasonBusinessModel>>(_unitOfWork.Repository<season>().GetList(p => p.leagueId == leagueId)).OrderByDescending(p => p.Id).ToList();
        }
    }
}
