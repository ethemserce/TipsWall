using System;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class StatisticBusinessModel
    {
        public long Id { get; set; }
        public long? FixtureId { get; set; }
        public long? LocalTeamId { get; set; }
        public long? VisitorTeamId { get; set; }
        public long? TypeId { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public int? LocalTeamValue { get; set; }
        public string LocalTeamName { get; set; }
        public int? VisitorTeamValue { get; set; }
        public string VisitorTeamName { get; set; }
        public string Location { get; set; }
        public string StatGroup { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public DateTime CreateDateTime { get; set; }
        public StatisticAnalysisBusinessModel StatisticAnalysis { get; set; }
    }
}
