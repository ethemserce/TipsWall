using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class fixture : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long leagueId { get; set; }
        public league league { get; set; }
        public long seasonId { get; set; }
        public season season { get; set; }
        public long stageId { get; set; }
        public stage stage { get; set; }
        public long? groupId { get; set; }
        public group group { get; set; }
        public long? aggregateId { get; set; }
        public aggregate aggregate { get; set; }
        public long? roundId { get; set; }
        public round round { get; set; }
        public long? stateId { get; set; }
        public state state { get; set; }
        public long? venueId { get; set; }
        public venue venue { get; set; }
        public long? localTeamId { get; set; }
        public team localTeam { get; set; }
        public long? visitorTeamId { get; set; }
        public team visitorTeam { get; set; }
        public int MyProperty { get; set; }
        public int? localTeamHtScore { get; set; }
        public int? visitorTeamHtScore { get; set; }
        public int? localTeamFtScore { get; set; }
        public int? visitorTeamFtScore { get; set; }
        public string name { get; set; }   
        public string resultInfo { get; set; }
        public string leg { get; set; }
        public string details { get; set; }
        public long? length { get; set; }
        public bool placeholder { get; set; }
        public bool hasOdds { get; set; }
        public bool hasPremiumOdds { get; set; }
        public string status { get; set; }
        public DateTime? startingAt { get; set; }
        public double startingAtTimestamp { get; set; }
        public ICollection<odd> odds { get; set; }
        public ICollection<bench> benches { get; set; }
        public ICollection<comment> comments { get; set; }
        public ICollection<commentary> commentaries { get; set; }
        public ICollection<corner> corners { get; set; }
        public ICollection<events> events { get; set; }
        public ICollection<formation> formations { get; set; }
    }
}
