using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class ScheduleFixture
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        [JsonProperty("stage_id")]
        public long StageId { get; set; }

        [JsonProperty("group_id")]
        public long GroupId { get; set; }

        [JsonProperty("aggregate_id")]
        public long AggregateId { get; set; }

        [JsonProperty("round_id")]
        public long RoundId { get; set; }

        [JsonProperty("state_id")]
        public long StateId { get; set; }

        [JsonProperty("venue_id")]
        public long VenueId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("result_info")]
        public string ResultInfo { get; set; }

        [JsonProperty("leg")]
        public string Leg { get; set; }

        [JsonProperty("details")]
        public object Details { get; set; }

        [JsonProperty("length")]
        public long Length { get; set; }

        [JsonProperty("placeholder")]
        public bool Placeholder { get; set; }

        [JsonProperty("has_odds")]
        public bool HasOdds { get; set; }
        [JsonProperty("starting_at")]
        public DateTimeOffset? StartingAt { get; set; }

        [JsonProperty("starting_at_timestamp")]
        public long StartingAtTimestamp { get; set; }

        [JsonProperty("participants")] // if add to url "include=participants"
        public List<Participant> Participants { get; set; }

        [JsonProperty("scores")] // if add to url "include=participants"
        public List<ScheduleScore> Scores { get; set; }
    }
}
