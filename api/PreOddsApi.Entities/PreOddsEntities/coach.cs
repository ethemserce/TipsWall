using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
   public class coach : BaseEntity
    {
        public long playerId { get; set; }
        public player player { get; set; }
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long countryId { get; set; }
        public country country { get; set; }
        public long nationalityId { get; set; }
        public long cityId { get; set; }
        public city city { get; set; }
        public string commonName { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string name { get; set; }
        public string displayName { get; set; }
        public string imagePath { get; set; }
        public int height { get; set; }
        public int weight { get; set; }
        public DateOnly DateOfBirth { get; set; }
        public ICollection<events> events { get; set; }
    }
}
