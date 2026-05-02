using PreOddsApi.Core.Model;
namespace PreOddsApi.Entities.PreOddsEntities
{
    public class cardscorer : BaseEntity
    {
        public long? leagueId { get; set; }
        public league league { get; set; }
        public long? playerId { get; set; }
        public player player { get; set; }
        public int? position { get; set; }
        public int? redcards { get; set; }
        public int? yellowcards { get; set; }
        public long? seasonId { get; set; }
        public season season { get; set; }
        public long? stageId { get; set; }
        public stage stage { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
    }
}
