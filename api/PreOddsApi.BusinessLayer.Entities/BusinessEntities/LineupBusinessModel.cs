using System;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class LineupBusinessModel
    {
        public long Id { get; set; }
        public long FixtureId { get; set; }
        public long PlayerId { get; set; }
        public long SportId { get; set; }
        public long TeamId { get; set; }
        public long PositionId { get; set; }
        public string FormationField { get; set; }
        public long TypeId { get; set; }
        public int? FormationPosition { get; set; }
        public string PlayerName { get; set; }
        public int? JerseyNumber { get; set; }
        public PlayerBusinessModel Player { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime UpdateDateTime { get; set; }
        //public int? AdditionalPosition { get; set; }
        //public int? FormationPosition { get; set; }
        //public int? Posx { get; set; }
        //public int? Posy { get; set; }
        //public int? StatsCardsRedcards { get; set; }
        //public int? StatsCardsYellowcards { get; set; }
        //public int? StatsFoulsCommitted { get; set; }
        //public int? StatsFoulsDrawn { get; set; }
        //public int? StatsGoalsConceded { get; set; }
        //public int? StatsGoalsScored { get; set; }
        //public int? StatsOtherAssists { get; set; }
        //public int? StatsOtherBlocks { get; set; }
        //public int? StatsOtherClearances { get; set; }
        //public int? StatsOtherHitWoodwork { get; set; }
        //public int? StatsOtherInterceptions { get; set; }
        //public int? StatsOtherMinutesPlayed { get; set; }
        //public int? StatsOtherOffsides { get; set; }
        //public int? StatsOtherPenCommitted { get; set; }
        //public int? StatsOtherPenMissed { get; set; }
        //public int? StatsOtherPenSaved { get; set; }
        //public int? StatsOtherPenScored { get; set; }
        //public int? StatsOtherPenWon { get; set; }
        //public int? StatsOtherSaves { get; set; }
        //public int? StatsOtherTackles { get; set; }
        //public int? StatsPassingCrossesAccuracy { get; set; }
        //public int? StatsPassingPasses { get; set; }
        //public int? StatsPassingPassesAccuracy { get; set; }
        //public int? StatsPassingTotalCrosses { get; set; }
        //public int? StatsShotsShotsOnGoal { get; set; }
        //public int? StatsShotsShotsTotal { get; set; }

        //public TeamBusinessModel Team { get; set; }

    }
}
