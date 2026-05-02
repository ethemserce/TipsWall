using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class FixtureForRoundBusinessModel
    {
        public long Id { get; set; }
        public int Attendance { get; set; }
        public int Commentaries { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string EtScore { get; set; }
        public string FtScore { get; set; }
        public long GroupId { get; set; }
        public GroupBusinessModel Group { get; set; }
        public string HtScore { get; set; }
        public long LeagueId { get; set; }
        public LeagueBusinessModel League { get; set; }
        public long LocalTeamCoachId { get; set; }
        public CoachBusinessModel LocalTeamCoach { get; set; }
        public string LocalTeamFormation { get; set; }
        public long LocalTeamId { get; set; }
        public TeamBusinessModel LocalTeam { get; set; }
        public int LocalTeamPenScore { get; set; }
        public int LocalTeamScore { get; set; }
        public string Pitch { get; set; }
        public long RefereeId { get; set; }
        public long RoundId { get; set; }
        public RoundBusinessModel Round { get; set; }
        public long SeasonId { get; set; }
        public SeasonBusinessModel Season { get; set; }
        public long StageId { get; set; }
        public StageBusinessModel Stage { get; set; }
        public string TimeStatus20 { get; set; }
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
        public DateTime UpdateDateTime { get; set; }
        public long VenueId { get; set; }
        public long VisitorTeamCoachId { get; set; }
        public CoachBusinessModel VisitorTeamCoach { get; set; }
        public string VisitorTeamFormation { get; set; }
        public long VisitorTeamId { get; set; }
        public TeamBusinessModel VisitorTeam { get; set; }
        public int VisitorTeamPenScore { get; set; }
        public int VisitorTeamScore { get; set; }
        public string WeatherClouds { get; set; }
        public string WeatherCode { get; set; }
        public string WeatherHumidity { get; set; }
        public string WeatherIcon { get; set; }
        public double WeatherTemperatureTemp { get; set; }
        public string WeatherTemperatureUnit { get; set; }
        public string WeatherType { get; set; }
        public int WeatherWindDegree { get; set; }
        public string WeatherWindSpeed { get; set; }
        public bool WinningOddsCalculated { get; set; }
        public int? IddaaCode { get; set; }
    }
}
