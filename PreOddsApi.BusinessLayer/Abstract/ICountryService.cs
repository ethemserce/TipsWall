using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ICountryService
    {
        CountryBusinessModel GetCountry(long countryId);
        CountryBusinessModel GetCountry(long countryId, int status);
        List<CountryBusinessModel> GetCountries();
        List<CountryBusinessModel> GetCountries(int status);
    }
}
