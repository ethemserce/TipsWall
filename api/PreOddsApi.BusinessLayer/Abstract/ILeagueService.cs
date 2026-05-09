using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ILeagueService
    {
        LeagueBusinessModel GetLeague(long leagueId);
        LeagueBusinessModel GetLeague(long leagueId, int status);
        List<LeagueBusinessModel> GetLeagues(long countryId, string lang);
        List<LeagueBusinessModel> GetFavoriteLeagues(string lang);
    }
}
