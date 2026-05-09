
namespace PreOddsApi.WebApi.Models.Team.V2Models
{
    public class TeamV2ViewModel
    {
        public long Id { get; set; }
        public string LogoPath { get; set; } = string.Empty;
        public string Name { get; set; }= string.Empty;
        public string ShortCode { get; set; } = string.Empty;
    }
}
