using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class team : BaseEntity
    {
        public long? sportId { get; set; }
        public sport sport { get; set; }
        public long? countryId { get; set; }
        public country country { get; set; }
        public long? venueId { get; set; }
        public venue venue { get; set; }
        public string gender { get; set; }
        public string name { get; set; }
        public string shortCode { get; set; }
        public string imagePath { get; set; }
        public long? founded { get; set; }
        public string type { get; set; }
        public bool? placeholder { get; set; }
        public DateTime? lastPlayedAt { get; set; }
        public ICollection<aggregate> aggregates { get; set; }
        public ICollection<assistscorer> assistscorers { get; set; }
        public ICollection<bench> benches { get; set; }
        public ICollection<cardscorer> cardscorers { get; set; }
        public ICollection<corner> corners { get; set; }
        public ICollection<events> events { get; set; }
        public ICollection<fixture> localTeamfixtures { get; set; }
        public ICollection<fixture> visitorTeamfixtures { get; set; }
        public ICollection<formation> formations { get; set; }
        public ICollection<goalscorer> goalscorers { get; set; }
    }
}
