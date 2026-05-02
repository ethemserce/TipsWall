using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
   public class SeasonBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long CurrentRoundId { get; set; }
        public bool CurrentSeason { get; set; }
        public long CurrentStageId { get; set; }
        public long LeagueId { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
