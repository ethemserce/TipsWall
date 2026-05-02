using PreOddsApi.WebUI.Models.Player.Models;
using PreOddsApi.WebUI.Models.Team.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Statistic.Models
{
    public class SeasonStatsViewModel
    {
        public int? NumberOfClubs { get; set; }
        public int? NumberOfMatches { get; set; }
        public int? NumberOfMatchesPlayed { get; set; }
        public int? NumberOfGoals { get; set; }
        public int? MatchesBothTeamsScored { get; set; }
        public int? NumberOfYellowcards { get; set; }
        public int? NumberOfYellowredcards { get; set; }
        public int? NumberOfRedcards { get; set; }
        public string AvgGoalsPerMatch { get; set; }
        public string AvgYellowcardsPerMatch { get; set; }
        public string AvgYellowredcardsPerMatch { get; set; }
        public string AvgRedcardsPerMatch { get; set; }
        public TeamViewModel TeamWithMostGoals { get; set; }
        public TeamViewModel TeamWithMostConcededGoals { get; set; }
        public TeamViewModel TeamWithMostGoalsPerMatch { get; set; }
        public PlayerViewModel SeasonTopscorer { get; set; }
        public PlayerViewModel SeasonAssistTopscorer { get; set; }
        public TeamViewModel TeamMostCleansheets { get; set; }
        public string GoalsScoredMinutes0 { get; set; }
        public string GoalsScoredMinutes15 { get; set; }
        public string GoalsScoredMinutes30 { get; set; }
        public string GoalsScoredMinutes45 { get; set; }
        public string GoalsScoredMinutes60 { get; set; }
        public string GoalsScoredMinutes75 { get; set; }
        public PlayerViewModel GoalkeeperMostCleansheets { get; set; }
        public int? GoalScoredEveryMinutes { get; set; }
    }
}
