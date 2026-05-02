using PreOddsApi.WebApi.Models.Player.V2Models;
using PreOddsApi.WebApi.Models.Team.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.TopScorer.V2Models
{
    public class SeasonStatsV2ViewModel
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
        public TeamV2ViewModel TeamWithMostGoals { get; set; }
        public TeamV2ViewModel TeamWithMostConcededGoals { get; set; }
        public TeamV2ViewModel TeamWithMostGoalsPerMatch { get; set; }
        public PlayerV2ViewModel SeasonTopscorer { get; set; }
        public PlayerV2ViewModel SeasonAssistTopscorer { get; set; }
        public TeamV2ViewModel TeamMostCleansheets { get; set; }
        public string GoalsScoredMinutes0 { get; set; }
        public string GoalsScoredMinutes15 { get; set; }
        public string GoalsScoredMinutes30 { get; set; }
        public string GoalsScoredMinutes45 { get; set; }
        public string GoalsScoredMinutes60 { get; set; }
        public string GoalsScoredMinutes75 { get; set; }
        public PlayerV2ViewModel GoalkeeperMostCleansheets { get; set; }
        public int? GoalScoredEveryMinutes { get; set; }
    }
}
