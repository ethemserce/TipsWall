using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Statistic.V2Models
{
    public class StatisticAnalysisV2ViewModel
    {
        public double AttacksAttacks { get; set; } = 0;
        public double BallSafe { get; set; } = 0;
        public double Corners { get; set; } = 0;
        public double Fouls { get; set; } = 0;
        public double FreeKick { get; set; } = 0;
        public double GoalAttempts { get; set; } = 0;
        public double GoalKick { get; set; } = 0;
        public double Offsides { get; set; } = 0;
        public double PassesAccurate { get; set; } = 0;
        public double PassesPercentage { get; set; } = 0;
        public double PassesTotal { get; set; } = 0;
        public double PossessionTime { get; set; } = 0;
        public double RedCards { get; set; } = 0;
        public double YellowCards { get; set; } = 0;
        public double Saves { get; set; } = 0;
        public double ShotsBlocked { get; set; } = 0;
        public double ShotsInsideBox { get; set; } = 0;
        public double ShotsOffGoal { get; set; } = 0;
        public double ShotsOnGoal { get; set; } = 0;
        public double ShotsOutsideBox { get; set; } = 0;
        public double ShotsTotal { get; set; } = 0;
        public double Substitutions { get; set; } = 0;
        public double ThrowIn { get; set; } = 0;
        public double GoalFor { get; set; }
        public double GoalAgainst { get; set; }
        public double GoalTotal { get; set; }
        public double GoalHTFor { get; set; }
        public double GoalHTAgainst { get; set; }
        public double GoalHTTotal { get; set; }
        public int TotalMatchCount { get; set; } = 0;
    }
}
