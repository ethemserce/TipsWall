using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class StandingBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public long LeagueId { get; set; }
        public long SeasonId { get; set; }
        public long StageId { get; set; }
        public long StandingsGroupId { get; set; }
        public TeamBusinessModel Team { get; set; }
        public long StandingsTeamId { get; set; }
        public int StandingsAwayDraw { get; set; }
        public int StandingsAwayGamesPlayed { get; set; }
        public int StandingsAwayGoalsAgainst { get; set; }
        public int StandingsAwayGoalsScored { get; set; }
        public int StandingsAwayLost { get; set; }
        public int StandingsAwayWon { get; set; }
        public int StandingsHomeDraw { get; set; }
        public int StandingsHomeGamesPlayed { get; set; }
        public int StandingsHomeGoalsAgainst { get; set; }
        public int StandingsHomeGoalsScored { get; set; }
        public int StandingsHomeLost { get; set; }
        public int StandingsHomeWon { get; set; }
        public int StandingsOverallDraw { get; set; }
        public int StandingsOverallGamesPlayed { get; set; }
        public int StandingsOverallGoalsAgainst { get; set; }
        public int StandingsOverallGoalsScored { get; set; }
        public int StandingsOverallLost { get; set; }
        public int StandingsOverallWon { get; set; }
        public int StandingsPositon { get; set; }
        public string StandingsRecentForm { get; set; }
        public string StandingsTotalGoalDifference { get; set; }
        public int StandingsTotalPoints { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
