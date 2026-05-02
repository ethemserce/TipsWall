using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
   public class LeagueBusinessModel
    {
        public long Id { get; set; }
        public CountryBusinessModel Country { get; set; }
        public long CountryId { get; set; }
        public DateTime CreateDateTime { get; set; }
        public bool Cup { get; set; }
        public int LegacyId { get; set; }
        public string Logo { get; set; }
        public LogoBusinessModel LogoSet { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public bool Favorite { get; set; }
        public int Status { get; set; }
    }
}
