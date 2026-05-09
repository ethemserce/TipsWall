using PreOddsApi.Core.Model;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class country : BaseEntity
    {
        public long continentId { get; set; }
        public continent continent { get; set; }
        public string name { get; set; }
        public string officialName { get; set; }
        public string fifaName { get; set; }
        public string iso2 { get; set; }
        public string iso3 { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public string imagePath { get; set; }
        public string borders { get; set; } // string array
        public ICollection<region> regions { get; set; }
        public ICollection<league> leagues { get; set; }
    }
}
