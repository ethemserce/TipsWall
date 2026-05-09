using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
   public class season : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long leagueId { get; set; }
        public league league { get; set; }
        public long tieBreakerRuleId { get; set; }
        public string name { get; set; }
        public bool finished { get; set; }
        public bool pending { get; set; }
        public bool isCurrent { get; set; }
        public DateTime? startingAt { get; set; }
        public DateTime? endingAt { get; set; }
        public DateTime? standingsRecalculatedAt { get; set; }
        public bool gamesInCurrentWeek { get; set; }
        public ICollection<stage> stages { get; set; }
        public ICollection<schedule> schedules { get; set; }
    }
}
