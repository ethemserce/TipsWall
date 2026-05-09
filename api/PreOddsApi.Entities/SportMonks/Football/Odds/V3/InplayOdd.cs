using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Odds.V3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class InplayOdd
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("external_id")]
        public long ExternalId { get; set; }

        [JsonProperty("fixture_id")]
        public long FixtureId { get; set; }

        [JsonProperty("market_id")]
        public long MarketId { get; set; }

        [JsonProperty("bookmaker_id")]
        public long BookmakerId { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("market_description")]
        public string MarketDescription { get; set; }

        [JsonProperty("probability")]
        public string Probability { get; set; }

        [JsonProperty("dp3")]
        public string Dp3 { get; set; }

        [JsonProperty("fractional")]
        public string Fractional { get; set; }

        [JsonProperty("american")]
        public string American { get; set; }

        [JsonProperty("winning")]
        public bool Winning { get; set; }

        [JsonProperty("suspended")]
        public bool Suspended { get; set; }

        [JsonProperty("stopped")]
        public bool Stopped { get; set; }

        [JsonProperty("total")]
        public string? Total { get; set; }

        [JsonProperty("handicap")]
        public string? Handicap { get; set; }

        [JsonProperty("participants")]
        public string? Participants { get; set; }

        [JsonProperty("market")] // if add to Query Params "include=market"
        public Market Market { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public Fixture Fixture { get; set; }

        [JsonProperty("bookmaker")] // if add to Query Params "include=bookmaker"
        public Bookmaker Bookmaker { get; set; }
    }
}
