using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class league : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long countryId { get; set; }
        public country country { get; set; }
        public string name { get; set; }
        public bool active { get; set; }
        public string shortCode { get; set; }
        public string imagePath { get; set; }
        public string type { get; set; }
        public string subType { get; set; }
        public DateTime lastPlayedAt { get; set; }
        public int category { get; set; }
        public bool hasJerseys { get; set; }
        public bool favorite { get; set; }
        public int status { get; set; }

        public ICollection<season> seasons { get; set; }
    }
}
