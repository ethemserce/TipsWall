using PreOddsApi.BusinessLayer.Entities.BusinessEntities.Fixture;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class FixtureBusinessModel
    {
        public FixtureBusinessModel()
        {
            this.Odds = new List<OddBusinessModel>();
        }

        public long Id { get; set; }
        public int Attendance { get; set; }
        public int Commentaries { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public long GroupId { get; set; }
        public GroupBusinessModel Group { get; set; } = new GroupBusinessModel();
        public string HtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueBusinessModel League { get; set; } = new LeagueBusinessModel();
        public CountryBusinessModel Country { get; set; } = new CountryBusinessModel();
        public List<StandingBusinessModel> Standing { get; set; } = new List<StandingBusinessModel>();
        public long LocalTeamCoachId { get; set; }
        public CoachBusinessModel LocalTeamCoach { get; set; } = new CoachBusinessModel();
        public List<LineupBusinessModel> LocalTeamBench { get; set; }=new List<LineupBusinessModel>();
        public List<CornerBusinessModel> LocalTeamCorner { get; set; } = new List<CornerBusinessModel>();
        public List<LineupBusinessModel> LocalTeamLineup { get; set; } = new List<LineupBusinessModel>();
        public List<SidelinedBusinessModel> LocalTeamSidelined { get; set; } = new List<SidelinedBusinessModel>();
        public List<StatisticBusinessModel> LocalTeamStatistic { get; set; } = new List<StatisticBusinessModel>();
        public StatisticAnalysisBusinessModel LocalTeamStatisticAnalysis { get; set; } = new StatisticAnalysisBusinessModel();
        public List<EventsBusinessModel> LocalTeamEvents { get; set; } = new List<EventsBusinessModel>();
        public List<FixtureForLeagueBusinessModel> LocalTeamLastMatches { get; set; } = new List<FixtureForLeagueBusinessModel>();
        public string LocalTeamFormation { get; set; }
        public string LocalTeamForm { get; set; }
        public long LocalTeamId { get; set; } = new long();
        public TeamBusinessModel LocalTeam { get; set; } = new TeamBusinessModel();
        public int LocalTeamPenScore { get; set; } = new int();
        public int LocalTeamScore { get; set; } = new int();
        public string Pitch { get; set; }
        public long RefereeId { get; set; } = new long();
        public RefereeBusinessModel Referee { get; set; } = new RefereeBusinessModel();
        public long RoundId { get; set; } = new long();
        public RoundBusinessModel Round { get; set; }
        public long SeasonId { get; set; } = new long();
        public SeasonBusinessModel Season { get; set; } = new SeasonBusinessModel();
        public long StageId { get; set; } = new long();
        public StageBusinessModel Stage { get; set; } = new StageBusinessModel();
        public int TimeAddedTime { get; set; } = new int();
        public int TimeExtraMinute { get; set; } = new int();
        public int TimeInjuryTime { get; set; } = new int();
        public int TimeMinute { get; set; }
        public string TimeStartingAtDate { get; set; }
        public string TimeStartingAtDateTime { get; set; }
        public string TimeStartingAtTime { get; set; }
        public int TimeStartingAtTimestamp { get; set; }
        public string TimeStartingAtTimezone { get; set; }
        public string TimeStatus { get; set; }
        public long VenueId { get; set; }
        public VenueBusinessModel Venue { get; set; }= new VenueBusinessModel();
        public long VisitorTeamCoachId { get; set; }
        public CoachBusinessModel VisitorTeamCoach { get; set; } = new CoachBusinessModel();
        public string VisitorTeamFormation { get; set; }
        public string VisitorTeamForm { get; set; }
        public long VisitorTeamId { get; set; }
        public List<StatisticBusinessModel> TeamsStatistics { get; set; } = new List<StatisticBusinessModel>();
        public TeamBusinessModel VisitorTeam { get; set; } = new TeamBusinessModel();
        public List<LineupBusinessModel> VisitorTeamBench { get; set; } = new List<LineupBusinessModel>();
        public List<CornerBusinessModel> VisitorTeamCorner { get; set; } = new List<CornerBusinessModel>();
        public List<LineupBusinessModel> VisitorTeamLineup { get; set; } = new List<LineupBusinessModel>();
        public List<SidelinedBusinessModel> VisitorTeamSidelined { get; set; } = new List<SidelinedBusinessModel>();
        public List<StatisticBusinessModel> VisitorTeamStatistic { get; set; } = new List<StatisticBusinessModel>();
        public StatisticAnalysisBusinessModel VisitorTeamStatisticAnalysis { get; set; } = new StatisticAnalysisBusinessModel();
        public SeasonStatsBusinessModel SeasonStats { get; set; } = new SeasonStatsBusinessModel();
        public List<EventsBusinessModel> VisitorTeamEvents { get; set; } = new List<EventsBusinessModel>();
        public List<FixtureForLeagueBusinessModel> VisitorTeamLastMatches { get; set; } = new List<FixtureForLeagueBusinessModel>();
        public List<EventsBusinessModel> TeamsEvents { get; set; } = new List<EventsBusinessModel>();
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
        public int? IddaaCode { get; set; }
        public List<CommentBusinessModel> Comment { get; set; }= new List<CommentBusinessModel>();
        public List<HighlightBusinessModel> Highlight { get; set; } = new List<HighlightBusinessModel>();
        public List<TvstationBusinessModel> Tvstation { get; set; } = new List<TvstationBusinessModel>();
        public DateTime UpdateDateTime { get; set; }
        public List<OddBusinessModel> Odds { get; set; } = new List<OddBusinessModel>();
        public List<FixtureForLeagueBusinessModel> HeadToHeadLastMatches { get; set; } = new List<FixtureForLeagueBusinessModel>();
    }
}
