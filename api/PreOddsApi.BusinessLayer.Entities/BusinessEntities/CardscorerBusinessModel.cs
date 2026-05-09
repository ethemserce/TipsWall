using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class CardscorerBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long LeagueId { get; set; }
        public long PlayerId { get; set; }
        public PlayerBusinessModel Player { get; set; }
        public int? Position { get; set; }
        public int? Redcards { get; set; }
        public int? Yellowcards { get; set; }
        public long SeasonId { get; set; }
        public long StageId { get; set; }
        public long TeamId { get; set; }
        public TeamBusinessModel Team { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
