using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class schedule : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long leagueId { get; set; }
        public league league { get; set; }
        public long seasonId { get; set; }
        public season season { get; set; }
        public long typeId { get; set; }
        public types types { get; set; }
        public string name { get; set; }
        public int sort_order { get; set; }
        public bool finished { get; set; }
        public bool is_current { get; set; }
        public DateTimeOffset starting_at { get; set; }
        public DateTimeOffset ending_at { get; set; }
        public bool games_in_current_week { get; set; }
        public int tie_breaker_rule_id { get; set; }
    }
}
