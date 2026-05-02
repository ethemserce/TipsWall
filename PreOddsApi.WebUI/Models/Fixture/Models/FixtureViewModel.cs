using PreOddsApi.WebUI.Models.Bench.Models;
using PreOddsApi.WebUI.Models.Coach.Models;
using PreOddsApi.WebUI.Models.Comment.Models;
using PreOddsApi.WebUI.Models.Continent.Models;
using PreOddsApi.WebUI.Models.Corner.Models;
using PreOddsApi.WebUI.Models.Country.Models;
using PreOddsApi.WebUI.Models.Events.Models;
using PreOddsApi.WebUI.Models.Group.Models;
using PreOddsApi.WebUI.Models.Highlight.Models;
using PreOddsApi.WebUI.Models.League.Models;
using PreOddsApi.WebUI.Models.Lineup.Models;
using PreOddsApi.WebUI.Models.Referee.Models;
using PreOddsApi.WebUI.Models.Round.Models;
using PreOddsApi.WebUI.Models.Season.Models;
using PreOddsApi.WebUI.Models.Sidelined.Models;
using PreOddsApi.WebUI.Models.Stage.Models;
using PreOddsApi.WebUI.Models.Standing.Models;
using PreOddsApi.WebUI.Models.Statistic.Models;
using PreOddsApi.WebUI.Models.Team.Models;
using PreOddsApi.WebUI.Models.Tvstation.Models;
using PreOddsApi.WebUI.Models.Venue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Fixture.Models
{
    public class FixtureViewModel
    {
        public long Id { get; set; }
        public int Attendance { get; set; }
        public int Commentaries { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public long GroupId { get; set; }
        public GroupViewModel Group { get; set; }
        public string HtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueViewModel League { get; set; }
        public CountryViewModel Country { get; set; }
        public ContinentViewModel Continent { get; set; }
        public List<StandingViewModel> Standing { get; set; }
        public long LocalTeamCoachId { get; set; }
        public CoachViewModel LocalTeamCoach { get; set; }
        public List<BenchViewModel> LocalTeamBench { get; set; }
        public List<CornerViewModel> LocalTeamCorner { get; set; }
        public List<LineupViewModel> LocalTeamLineup { get; set; }
        public List<SidelinedViewModel> LocalTeamSidelined { get; set; }
        public List<StatisticViewModel> LocalTeamStatistic { get; set; }
        public StatisticAnalysisViewModel LocalTeamStatisticAnalysis { get; set; }
        public List<EventsViewModel> LocalTeamEvents { get; set; }
        public List<FixtureForLeagueViewModel> LocalTeamLastMatches { get; set; }
        public string LocalTeamFormation { get; set; }
        public string LocalTeamForm { get; set; }
        public long LocalTeamId { get; set; }
        public TeamViewModel LocalTeam { get; set; }
        public int LocalTeamPenScore { get; set; }
        public int LocalTeamScore { get; set; }
        public string Pitch { get; set; }
        public long RefereeId { get; set; }
        public RefereeViewModel Referee { get; set; }
        public long RoundId { get; set; }
        public RoundViewModel Round { get; set; }
        public long SeasonId { get; set; }
        public SeasonViewModel Season { get; set; }
        public long StageId { get; set; }
        public StageViewModel Stage { get; set; }
        public int TimeAddedTime { get; set; }
        public int TimeExtraMinute { get; set; }
        public int TimeInjuryTime { get; set; }
        public int TimeMinute { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStartingAtTime { get; set; }
        public int TimeStartingAtTimestamp { get; set; }
        public string TimeStartingAtTimezone { get; set; }
        public string TimeStatus { get; set; }
        public long VenueId { get; set; }
        public VenueViewModel Venue { get; set; }
        public long VisitorTeamCoachId { get; set; }
        public CoachViewModel VisitorTeamCoach { get; set; }
        public string VisitorTeamFormation { get; set; }
        public string VisitorTeamForm { get; set; }
        public long VisitorTeamId { get; set; }
        public TeamViewModel VisitorTeam { get; set; }
        public List<BenchViewModel> VisitorTeamBench { get; set; }
        public List<CornerViewModel> VisitorTeamCorner { get; set; }
        public List<LineupViewModel> VisitorTeamLineup { get; set; }
        public List<SidelinedViewModel> VisitorTeamSidelined { get; set; }
        public List<StatisticViewModel> VisitorTeamStatistic { get; set; }
        public StatisticAnalysisViewModel VisitorTeamStatisticAnalysis { get; set; }
        public List<EventsViewModel> VisitorTeamEvents { get; set; }
        public List<FixtureForLeagueViewModel> VisitorTeamLastMatches { get; set; }
        public int VisitorTeamPenScore { get; set; }
        public int VisitorTeamScore { get; set; }
        public string WeatherClouds { get; set; }
        public string WeatherCode { get; set; }
        public string WeatherHumidity { get; set; }
        public string WeatherIcon { get; set; }
        public double WeatherTemperatureTemp { get; set; }
        public string WeatherTemperatureUnit { get; set; }
        public double WeatherTemperatureCelsiusTemp { get; set; }
        public string WeatherType { get; set; }
        public int WeatherWindDegree { get; set; }
        public string WeatherWindSpeed { get; set; }
        public bool WinningOddsCalculated { get; set; }
        public List<CommentViewModel> Comment { get; set; }
        public List<HighlightViewModel> Highlight { get; set; }
        public List<TvstationViewModel> Tvstation { get; set; }
        public List<FixtureForLeagueViewModel> HeadToHeadLastMatches { get; set; }
        public List<EventsViewModel> EventList { get; set; }
        public List<CornerViewModel> CornerList { get; set; }
        public bool RunTimer { get; set; }
    }
}
