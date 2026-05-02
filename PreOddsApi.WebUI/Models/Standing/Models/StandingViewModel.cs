using PreOddsApi.WebUI.Models.Team.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Standing.Models
{
    public class StandingViewModel
    {
        public long LeagueId { get; set; }
        public long SeasonId { get; set; }
        public long StageId { get; set; }
        public int StandingsAwayDraw { get; set; }
        public int StandingsAwayGamesPlayed { get; set; }
        public int StandingsAwayGoalsAgainst { get; set; }
        public int StandingsAwayGoalsScored { get; set; }
        public int StandingsAwayLost { get; set; }
        public int StandingsAwayWon { get; set; }
        public long StandingsGroupId { get; set; }
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
        public TeamViewModel Team { get; set; }
        public long StandingsTeamId { get; set; }
        public string StandingsTotalGoalDifference { get; set; }
        public int StandingsTotalPoints { get; set; }
    }
}
