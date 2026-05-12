using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football
{
    public class TvStation : SportMonksBaseEntity
    {
        // SportMonks reuses the same C# class for two different JSON shapes:
        //  - /v3/football/tv-stations: { id: 37, name: "ESPN", ... } where
        //    `id` is the canonical broadcaster id.
        //  - fixture-include `tvstations`: { id: 118282498, tvstation_id: 37,
        //    country_id: 5, ... } where `id` is the per-(fixture,country)
        //    link row and `tvstation_id` points at the canonical broadcaster.
        // TvstationId is null on the standalone shape — callers should
        // resolve a canonical id with `TvstationId ?? Id`.
        [JsonProperty("tvstation_id")]
        public long? TvstationId { get; set; }

        [JsonProperty("country_id")]
        public long? CountryId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("image_path")]
        public string ImagePath { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("related_id")]
        public long? RelatedId { get; set; }

        [JsonProperty("countries")]
        public List<Country> Countries { get; set; }

        [JsonProperty("fixtures")]
        public List<Fixture> Fixtures { get; set; }

    }
}
