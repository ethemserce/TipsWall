using PreOddsApi.WebUI.Models.Player.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Sidelined.Models
{
    public class SidelinedViewModel
    {
        public long Id { get; set; }
        public int AdditionalPosition { get; set; }
        public long FixtureId { get; set; }
        public int FormationPosition { get; set; }
        public int Number { get; set; }
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public PlayerViewModel Player { get; set; }
        public string Position { get; set; }
        public int Posx { get; set; }
        public int Posy { get; set; }
        public int StatsCardsRedcards { get; set; }
        public int StatsCardsYellowcards { get; set; }
        public int StatsFoulsCommitted { get; set; }
        public int StatsFoulsDrawn { get; set; }
        public int StatsGoalsConceded { get; set; }
        public int StatsGoalsScored { get; set; }
        public int StatsOtherAssists { get; set; }
        public int StatsOtherBlocks { get; set; }
        public int StatsOtherClearances { get; set; }
        public int StatsOtherHitWoodwork { get; set; }
        public int StatsOtherInterceptions { get; set; }
        public int StatsOtherMinutesPlayed { get; set; }
        public int StatsOtherOffsides { get; set; }
        public int StatsOtherPenCommitted { get; set; }
        public int StatsOtherPenMissed { get; set; }
        public int StatsOtherPenSaved { get; set; }
        public int StatsOtherPenScored { get; set; }
        public int StatsOtherPenWon { get; set; }
        public int StatsOtherSaves { get; set; }
        public int StatsOtherTackles { get; set; }
        public int StatsPassingCrossesAccuracy { get; set; }
        public int StatsPassingPasses { get; set; }
        public int StatsPassingPassesAccuracy { get; set; }
        public int StatsPassingTotalCrosses { get; set; }
        public int StatsShotsShotsOnGoal { get; set; }
        public int StatsShotsShotsTotal { get; set; }
        public long TeamId { get; set; }
    }
}
