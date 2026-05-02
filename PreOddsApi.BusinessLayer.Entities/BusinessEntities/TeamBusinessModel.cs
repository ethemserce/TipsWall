using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class TeamBusinessModel
    {
        public long Id { get; set; }
        public long CountryId { get; set; }
        public CountryBusinessModel Country { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int Founded { get; set; }
        public int LegacyId { get; set; }
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public bool NationalTeam { get; set; }
        public string ShortCode { get; set; }
        public string Twitter { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public long VenueId { get; set; }
        public VenueBusinessModel Venue { get; set; }
    }
}
