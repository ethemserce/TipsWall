using PreOddsApi.BusinessLayer.Entities.BusinessEntities;
using PreOddsApi.WebApi.Models.Bench.V2Models;
using PreOddsApi.WebApi.Models.Coach.V2Models;
using PreOddsApi.WebApi.Models.Comment.V2Models;
using PreOddsApi.WebApi.Models.Continent.V2Models;
using PreOddsApi.WebApi.Models.Corner.V2Models;
using PreOddsApi.WebApi.Models.Country.V2Models;
using PreOddsApi.WebApi.Models.Events.V2Models;
using PreOddsApi.WebApi.Models.Group.V2Models;
using PreOddsApi.WebApi.Models.Highlight.V2Models;
using PreOddsApi.WebApi.Models.League.V2Models;
using PreOddsApi.WebApi.Models.Lineup.V2Models;
using PreOddsApi.WebApi.Models.Referee.V2Models;
using PreOddsApi.WebApi.Models.Round.V2Models;
using PreOddsApi.WebApi.Models.Season.V2Models;
using PreOddsApi.WebApi.Models.Sidelined.V2Models;
using PreOddsApi.WebApi.Models.Stage.V2Models;
using PreOddsApi.WebApi.Models.Standing.V2Models;
using PreOddsApi.WebApi.Models.Statistic.V2Models;
using PreOddsApi.WebApi.Models.Team.V2Models;
using PreOddsApi.WebApi.Models.TopScorer.V2Models;
using PreOddsApi.WebApi.Models.TvStation.V2Models;
using PreOddsApi.WebApi.Models.Venue.V2Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture.V2Models
{
    public class FixtureV2ViewModel
    {
        public long Id { get; set; }
        public int Attendance { get; set; }
        public int Commentaries { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public long GroupId { get; set; }
        public GroupV2ViewModel Group { get; set; }= new GroupV2ViewModel();
        public string HtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueV2ViewModel League { get; set; } = new LeagueV2ViewModel();
        public CountryV2ViewModel Country { get; set; } = new CountryV2ViewModel();
        public ContinentV2ViewModel Continent { get; set; } = new ContinentV2ViewModel();
        public List<StandingV2ViewModel> Standing { get; set; }= new List<StandingV2ViewModel>();
        public long LocalTeamCoachId { get; set; }
        public CoachV2ViewModel LocalTeamCoach { get; set; } = new CoachV2ViewModel();
        public List<BenchV2ViewModel> LocalTeamBench { get; set; } = new List<BenchV2ViewModel>();
        public List<CornerV2ViewModel> LocalTeamCorner { get; set; } = new List<CornerV2ViewModel>();
        public List<LineupV2ViewModel> LocalTeamLineup { get; set; } = new List<LineupV2ViewModel>();
        public List<SidelinedV2ViewModel> LocalTeamSidelined { get; set; } = new List<SidelinedV2ViewModel>();
        public List<StatisticV2ViewModel> LocalTeamStatistic { get; set; } = new List<StatisticV2ViewModel>();
        public StatisticAnalysisV2ViewModel LocalTeamStatisticAnalysis { get; set; } = new StatisticAnalysisV2ViewModel();
        public SeasonStatsV2ViewModel SeasonStats { get; set; } = new SeasonStatsV2ViewModel();
        public List<EventsV2ViewModel> LocalTeamEvents { get; set; } = new List<EventsV2ViewModel>();
        public List<FixtureForLeagueV2ViewModel> LocalTeamLastMatches { get; set; } = new List<FixtureForLeagueV2ViewModel>();
        public List<StatisticV2ViewModel> TeamsStatistics { get; set; } = new  List<StatisticV2ViewModel>();
        public string LocalTeamFormation { get; set; }
        public string LocalTeamForm { get; set; }
        public long LocalTeamId { get; set; }
        public TeamV2ViewModel LocalTeam { get; set; } = new TeamV2ViewModel();
        public int LocalTeamPenScore { get; set; }
        public int LocalTeamScore { get; set; }
        public string Pitch { get; set; }
        public long RefereeId { get; set; }
        public RefereeV2ViewModel Referee { get; set; } = new RefereeV2ViewModel();
        public long RoundId { get; set; }
        public RoundV2ViewModel Round { get; set; } = new RoundV2ViewModel();
        public long SeasonId { get; set; }
        public SeasonV2ViewModel Season { get; set; } = new SeasonV2ViewModel();
        public long StageId { get; set; }
        public StageV2ViewModel Stage { get; set; } = new StageV2ViewModel();
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
        public VenueV2ViewModel Venue { get; set; } = new VenueV2ViewModel();
        public long VisitorTeamCoachId { get; set; }
        public CoachV2ViewModel VisitorTeamCoach { get; set; } = new CoachV2ViewModel();
        public string VisitorTeamFormation { get; set; }
        public string VisitorTeamForm { get; set; }
        public long VisitorTeamId { get; set; }
        public TeamV2ViewModel VisitorTeam { get; set; } = new TeamV2ViewModel();
        public List<BenchV2ViewModel> VisitorTeamBench { get; set; } = new List<BenchV2ViewModel>();
        public List<CornerV2ViewModel> VisitorTeamCorner { get; set; } = new List<CornerV2ViewModel>();
        public List<LineupV2ViewModel> VisitorTeamLineup { get; set; } = new List<LineupV2ViewModel>();
        public List<SidelinedV2ViewModel> VisitorTeamSidelined { get; set; } = new List<SidelinedV2ViewModel>();
        public List<StatisticV2ViewModel> VisitorTeamStatistic { get; set; } = new List<StatisticV2ViewModel>();
        public StatisticAnalysisV2ViewModel VisitorTeamStatisticAnalysis { get; set; } = new StatisticAnalysisV2ViewModel();
        public List<EventsV2ViewModel> VisitorTeamEvents { get; set; } = new List<EventsV2ViewModel>();
        public List<FixtureForLeagueV2ViewModel> VisitorTeamLastMatches { get; set; } = new List<FixtureForLeagueV2ViewModel>();
        public List<EventsV2ViewModel> TeamsEvents { get; set; } = new List<EventsV2ViewModel>();
        public List<CornerV2ViewModel> TeamsCorners { get; set; } = new List<CornerV2ViewModel>();
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
        public List<CommentV2ViewModel> Comment { get; set; } = new List<CommentV2ViewModel>();
        public List<HighlightV2ViewModel> Highlight { get; set; } = new List<HighlightV2ViewModel>();
        public List<TvstationV2ViewModel> Tvstation { get; set; } = new List<TvstationV2ViewModel>();
        public List<FixtureForLeagueV2ViewModel> HeadToHeadLastMatches { get; set; } = new List<FixtureForLeagueV2ViewModel>(); 
    }
}
