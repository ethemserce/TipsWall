using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Core.V3
{
    public class Metadata
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("metadatable_type")]
        public string MetadatableType { get; set; }

        [JsonProperty("metadatable_id")]
        public int MetadatableId { get; set; }

        [JsonProperty("type_id")]
        public long TypeId { get; set; }

        [JsonProperty("value")]
        public object Value { get; set; }

        [JsonProperty("value_type")]
        public string ValueType { get; set; }
    }
}
