using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
   public class SeasonStatsBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long LeagueId { get; set; }
        public long SeasonId { get; set; }
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
        public TeamBusinessModel TeamWithMostGoals { get; set; }
        public TeamBusinessModel TeamWithMostConcededGoals { get; set; }
        public TeamBusinessModel TeamWithMostGoalsPerMatch { get; set; }
        public PlayerBusinessModel SeasonTopscorer { get; set; }
        public PlayerBusinessModel SeasonAssistTopscorer { get; set; }
        public TeamBusinessModel TeamMostCleansheets { get; set; }
        public string GoalsScoredMinutes0 { get; set; }
        public string GoalsScoredMinutes15 { get; set; }
        public string GoalsScoredMinutes30 { get; set; }
        public string GoalsScoredMinutes45 { get; set; }
        public string GoalsScoredMinutes60 { get; set; }
        public string GoalsScoredMinutes75 { get; set; }
        public PlayerBusinessModel GoalkeeperMostCleansheets { get; set; }
        public int? GoalScoredEveryMinutes { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
