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
    public class LeagueService : ILeagueService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICountryService _countryService;

        public LeagueService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, ICountryService countryService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _countryService = countryService;
        }

        public LeagueBusinessModel GetLeague(long leagueId)
        {
            var league = _mapper.Map<LeagueBusinessModel>(_unitOfWork.Repository<league>().Get(p => p.id == leagueId));
            if (league != null)
            {
                LogoSet(league);
                league.Country = _countryService.GetCountry(league.CountryId);
            }
            return league;
        }

        public LeagueBusinessModel GetLeague(long leagueId, int status)
        {
            var league = _mapper.Map<LeagueBusinessModel>(_unitOfWork.Repository<league>().Get(p => p.id == leagueId && p.status == status));
            if (league != null)
            {
                LogoSet(league);
                league.Country = _countryService.GetCountry(league.CountryId);
            }
            return league;
        }

        public List<LeagueBusinessModel> GetLeagues(long countryId, string lang)
        {
            var leagues = _mapper.Map<List<LeagueBusinessModel>>(_unitOfWork.Repository<league>().GetList(p => p.countryId == countryId));
            foreach (var item in leagues)
            {
                if (item != null) { LogoSet(item); }
            }

            return leagues.OrderBy(p => p.Id).ToList();
        }

        public List<LeagueBusinessModel> GetFavoriteLeagues(string lang)
        {
            var leagues = _mapper.Map<List<LeagueBusinessModel>>(_unitOfWork.Repository<league>().GetList(p => p.favorite == true));

            foreach (var item in leagues)
            {
                if (item != null) { LogoSet(item); }
            }
            return leagues.OrderBy(p => p.Id).ToList();
        }

        private void LogoSet(LeagueBusinessModel league)
        {
            if (!string.IsNullOrEmpty(league.Logo))
            {
                league.LogoSet = new LogoBusinessModel()
                {
                    Logo16 = league.Logo.Replace("/64/", "/16/"),
                    Logo24 = league.Logo.Replace("/64/", "/24/"),
                    Logo32 = league.Logo.Replace("/64/", "/32/"),
                    Logo48 = league.Logo.Replace("/64/", "/48/"),
                    Logo64 = league.Logo.Replace("/64/", "/64/"),
                    Logo128 = league.Logo.Replace("/64/", "/128/"),
                    Logo256 = league.Logo.Replace("/64/", "/256/"),
                    Logo512 = league.Logo.Replace("/64/", "/512/")
                };
            }
            else
            {
                league.Logo = ""; // "~/images/leagues/Unknown.png";
                league.LogoSet = new LogoBusinessModel()
                {
                    Logo16 = league.Logo.Replace("/64/", "/16/"),
                    Logo24 = league.Logo.Replace("/64/", "/24/"),
                    Logo32 = league.Logo.Replace("/64/", "/32/"),
                    Logo48 = league.Logo.Replace("/64/", "/48/"),
                    Logo64 = league.Logo.Replace("/64/", "/64/"),
                    Logo128 = league.Logo.Replace("/64/", "/128/"),
                    Logo256 = league.Logo.Replace("/64/", "/256/"),
                    Logo512 = league.Logo.Replace("/64/", "/512/")
                };
            }
        }
    }
}