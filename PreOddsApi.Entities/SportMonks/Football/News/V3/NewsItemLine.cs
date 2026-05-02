using Newtonsoft.Json;

namespace PreOddsApi.Entities.SportMonks.Football
{
    public class NewsItemLine : SportMonksBaseEntity
    {
        [JsonProperty("newsitem_id")]
        public long NewsitemId { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
