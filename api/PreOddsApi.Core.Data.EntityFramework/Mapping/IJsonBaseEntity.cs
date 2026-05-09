using Newtonsoft.Json;
namespace PreOddsApi.Core.Data.EntityFramework.Mapping
{
    public interface IJsonBaseEntity
    {
        [JsonProperty("id")]
        public long Id { get; set; }
    }
}
