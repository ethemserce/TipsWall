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
    public class CountryService : ICountryService
    {
        private readonly IUnitOfWork<PreOddsApiDbContext> _unitOfWork;
        private readonly IMapper _mapper;

        public CountryService(IUnitOfWork<PreOddsApiDbContext> unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public CountryBusinessModel GetCountry(long countryId)
        {
            var country = _mapper.Map<CountryBusinessModel>(_unitOfWork.Repository<country>().Get(p => p.id == countryId));
            if (country != null) { LogoSet(country); }
            var continent = _unitOfWork.Repository<continent>().Get(p => p.id == country.ContinentId);

            return country;
        }

        public CountryBusinessModel GetCountry(long countryId, int status)
        {
            var country = _mapper.Map<CountryBusinessModel>(_unitOfWork.Repository<country>().Get(p => p.id == countryId));
            if (country != null) { LogoSet(country); }
            var continent = _unitOfWork.Repository<continent>().Get(p => p.id == country.ContinentId);

            return country;
        }

        public List<CountryBusinessModel> GetCountries()
        {
            var query = _mapper.Map<List<CountryBusinessModel>>(_unitOfWork.Repository<country>().GetList());
            foreach (var country in query)
            {
                if (country != null) { LogoSet(country); }
            }

            return query;
        }

        public List<CountryBusinessModel> GetCountries(int status)
        {
            var query = _mapper.Map<List<CountryBusinessModel>>(_unitOfWork.Repository<country>().GetList());
            foreach (var country in query)
            {
                if (country != null) { LogoSet(country); }
            }

            return query;
        }

        private void LogoSet(CountryBusinessModel country)
        {
            //if (!string.IsNullOrEmpty(country.Logo))
            //{
            //    country.LogoSet = new LogoBusinessModel()
            //    {
            //        Logo16 = country.Logo.Replace("/64/", "/16/"),
            //        Logo24 = country.Logo.Replace("/64/", "/24/"),
            //        Logo32 = country.Logo.Replace("/64/", "/32/"),
            //        Logo48 = country.Logo.Replace("/64/", "/48/"),
            //        Logo64 = country.Logo.Replace("/64/", "/64/"),
            //        Logo128 = country.Logo.Replace("/64/", "/128/"),
            //        Logo256 = country.Logo.Replace("/64/", "/256/"),
            //        Logo512 = country.Logo.Replace("/64/", "/512/")
            //    };
            //}
            //else
            //{
            //    country.Logo = "~/images/leagues/Unknown.png";
            //    country.LogoSet = new LogoBusinessModel()
            //    {
            //        Logo16 = country.Logo.Replace("/64/", "/16/"),
            //        Logo24 = country.Logo.Replace("/64/", "/24/"),
            //        Logo32 = country.Logo.Replace("/64/", "/32/"),
            //        Logo48 = country.Logo.Replace("/64/", "/48/"),
            //        Logo64 = country.Logo.Replace("/64/", "/64/"),
            //        Logo128 = country.Logo.Replace("/64/", "/128/"),
            //        Logo256 = country.Logo.Replace("/64/", "/256/"),
            //        Logo512 = country.Logo.Replace("/64/", "/512/")
            //    };
            //}
        }
    }
}
