using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Team
{
    public class TeamViewModel
    {
        public long Id { get; set; }
        public long CountryId { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int Founded { get; set; }
        public int LegacyId { get; set; }
        public string LogoPath { get; set; }
        public string Name { get; set; }
        public bool NationalTeam { get; set; }
        public string ShortCode { get; set; }
        public string Twitter { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public long VenueId { get; set; }
    }
}
