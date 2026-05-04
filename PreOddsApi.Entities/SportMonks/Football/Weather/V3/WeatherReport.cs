using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Football.V3;

namespace PreOddsApi.Entities.SportMonks.Football.Weather.V3
{
    public class WeatherReport : SportMonksBaseEntity
    {
        [JsonProperty("fixture_id")]
        public long? FixtureId { get; set; }

        [JsonProperty("venue_id")]
        public long? VenueId { get; set; }

        [JsonProperty("temperature")]
        public object Temperature { get; set; }

        [JsonProperty("feels_like")]
        public object FeelsLike { get; set; }

        [JsonProperty("wind")]
        public object Wind { get; set; }

        [JsonProperty("humidity")]
        public string Humidity { get; set; }

        [JsonProperty("pressure")]
        public int? Pressure { get; set; }

        [JsonProperty("clouds")]
        public string Clouds { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("metric")]
        public string Metric { get; set; }

        [JsonProperty("current")]
        public object Current { get; set; }

        [JsonProperty("venue")] // if add to Query Params "include=venue"
        public Venue Venue { get; set; }

        [JsonProperty("fixture")] // if add to Query Params "include=fixture"
        public Fixture Fixture { get; set; }
    }
}
