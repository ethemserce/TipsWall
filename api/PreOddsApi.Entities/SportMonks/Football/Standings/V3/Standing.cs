using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.Standings.V3;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Standing : SportMonksBaseEntity
    {

        [JsonProperty("participant_id")]
        public long? ParticipantId { get; set; }

        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("league_id")]
        public long? LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long? SeasonId { get; set; }

        [JsonProperty("stage_id")]
        public long? StageId { get; set; }

        [JsonProperty("group_id")]
        public long? GroupId { get; set; }

        [JsonProperty("round_id")]
        public long? RoundId { get; set; }

        [JsonProperty("standing_rule_id")]
        public long? StandingRuleId { get; set; }

        [JsonProperty("position")]
        public int? Position { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("points")]
        public int? Points { get; set; }

        [JsonProperty("participant")] // if add to Query Params "include=participant"
        public Participant Participant { get; set; }

        [JsonProperty("league")] // if add to Query Params "include=league"
        public League League { get; set; }

        [JsonProperty("season")] // if add to Query Params "include=season"
        public Season Season { get; set; }

        [JsonProperty("stage")] // if add to Query Params "include=state"
        public Stage Stage { get; set; }

        [JsonProperty("group")] // if add to Query Params "include=group"
        public Group Group { get; set; }

        [JsonProperty("round")] // if add to Query Params "include=round"
        public Round Round { get; set; }

        [JsonProperty("rule")] // if add to Query Params "include=rule"
        public StandingRule StandingRule { get; set; }

        [JsonProperty("details")] // if add to Query Params "include=details"
        public List<StandingDetail> StandingDetail { get; set; }

        [JsonProperty("form")] // if add to Query Params "include=form"
        public List<StandingForm> StandingForm { get; set; }

        [JsonProperty("sport")] // if add to Query Params "include=sport"
        public Sport Sport { get; set; }
    }
}
