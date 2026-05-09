using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Statistic.V2Models
{
    public class StatisticV2ViewModel
    {
        public long Id { get; set; }
        public long? FixtureId { get; set; }
        public long? LocalTeamId { get; set; }
        public long? VisitorTeamId { get; set; }
        public long? TypeId { get; set; }
        public string Type { get; set; }
        public string TypeName { get; set; }
        public int? LocalTeamValue { get; set; }
        public int? VisitorTeamValue { get; set; }
        public string LocalTeamName { get; set; }
        public string VisitorTeamName { get; set; }
        public string Location { get; set; }
        public string StatGroup { get; set; }

        //public long Id { get; set; }
        //public int AttacksAttacks { get; set; } = 0;
        //public int BallSafe { get; set; } = 0;
        //public int Corners { get; set; } = 0;
        //public long FixtureId { get; set; }
        //public int Fouls { get; set; } = 0;
        //public int FreeKick { get; set; } = 0;
        //public int GoalAttempts { get; set; } = 0;
        //public int GoalKick { get; set; } = 0;
        //public int Offsides { get; set; } = 0;
        //public int PassesAccurate { get; set; } = 0;
        //public int PassesPercentage { get; set; } = 0;
        //public int PassesTotal { get; set; } = 0;
        //public int PossessionTime { get; set; } = 0;
        //public int RedCards { get; set; } = 0;
        //public int YellowCards { get; set; } = 0;
        //public int Saves { get; set; } = 0;
        //public int ShotsBlocked { get; set; } = 0;
        //public int ShotsInsideBox { get; set; } = 0;
        //public int ShotsOffGoal { get; set; } = 0;
        //public int ShotsOnGoal { get; set; } = 0;
        //public int ShotsOutsideBox { get; set; } = 0;
        //public int ShotsTotal { get; set; } = 0;
        //public int Substitutions { get; set; } = 0;
        //public int ThrowIn { get; set; } = 0;
        //public long TeamId { get; set; }
    }
}
