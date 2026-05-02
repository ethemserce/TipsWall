using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Statistic.Models
{
    public class StatisticViewModel
    {
        public long Id { get; set; }
        public int AttacksAttacks { get; set; }
        public int BallSafe { get; set; }
        public int Corners { get; set; }
        public long FixtureId { get; set; }
        public int Fouls { get; set; }
        public int FreeKick { get; set; }
        public int GoalAttempts { get; set; }
        public int GoalKick { get; set; }
        public int Offsides { get; set; }
        public int PassesAccurate { get; set; }
        public int PassesPercentage { get; set; }
        public int PassesTotal { get; set; }
        public int PossessionTime { get; set; }
        public int RedCards { get; set; }
        public int YellowCards { get; set; }
        public int Saves { get; set; }
        public int ShotsBlocked { get; set; }
        public int ShotsInsideBox { get; set; }
        public int ShotsOffGoal { get; set; }
        public int ShotsOnGoal { get; set; }
        public int ShotsOutsideBox { get; set; }
        public int ShotsTotal { get; set; }
        public int Substitutions { get; set; }
        public int ThrowIn { get; set; }
        public long TeamId { get; set; }
    }
}
