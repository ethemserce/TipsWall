using Newtonsoft.Json;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.Entities.SportMonks
{
    public class SportMonksBaseEntity : IJsonBaseEntity
    {
        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
