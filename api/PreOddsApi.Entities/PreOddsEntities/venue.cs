using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class venue : BaseEntity
    {
        public long countryId { get; set; }
        public country country { get; set; }
        public long cityId { get; set; }
        public city city { get; set; }
        public string name { get; set; }
        public string address { get; set; }
        public string zipcode { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public int capacity { get; set; }
        public string imagePath { get; set; }
        public string cityName { get; set; }
        public string surface { get; set; }
        public bool nationalTeam { get; set; }
        public ICollection<fixture> fixtures { get; set; }
    }
}
