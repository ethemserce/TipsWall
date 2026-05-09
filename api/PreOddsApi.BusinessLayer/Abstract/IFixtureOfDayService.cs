using PreOddsApi.BusinessLayer.Entities.BusinessEntities.FixtureOfDay;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Abstract
{
    public interface IFixtureOfDayService
    {
        List<FixtureOfDayBusinessModel> GetFixtureOfDay(string date, string timezone);
    }
}
