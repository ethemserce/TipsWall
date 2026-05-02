using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Common.Enums;
using System;

namespace PreOddsApi.Entities.SportMonks.Core.Common.V3
{
    public class Participant : SportMonksBaseEntity
    {
        [JsonProperty("sport_id")]
        public long? SportId { get; set; }

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }

        [JsonProperty("venue_id")]
        public long? VenueId { get; set; }

        [JsonProperty("gender")]
        public Gender? Gender { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("short_code")]
        public string ShortCode { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        [JsonProperty("founded")]
        public long? Founded { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("placeholder")]
        public bool? Placeholder { get; set; }

        [JsonProperty("last_played_at")]
        public DateTime? LastPlayedAt { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }
}
