using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface ISeasonService
    {
        SeasonBusinessModel GetSeason(long seasonId);
        SeasonBusinessModel GetCurrentSeason(long leagueId);
        List<SeasonBusinessModel> GetSeasons(long leagueId);
    }
}
