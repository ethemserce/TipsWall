using Newtonsoft.Json;
using PreOddsApi.Entities.PreOddsEntities;
using PreOddsApi.Entities.SportMonks.Core.Common.V3;
using PreOddsApi.Entities.SportMonks.Core.V3;
using PreOddsApi.Entities.SportMonks.Football.Statistics.V3;
using PreOddsApi.Entities.SportMonks.Football.Weather.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class Fixture : SportMonksBaseEntity
    {

        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        [JsonProperty("stage_id")]
        public long StageId { get; set; }

        [JsonProperty("group_id")]
        public long? GroupId { get; set; }

        [JsonProperty("aggregate_id")]
        public long? AggregateId { get; set; }

        [JsonProperty("round_id")]
        public long? RoundId { get; set; }

        [JsonProperty("state_id")]
        public long StateId { get; set; }

        [JsonProperty("venue_id")]
        public long? VenueId { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("result_info")]
        public string? ResultInfo { get; set; }

        [JsonProperty("leg")]
        public string Leg { get; set; }

        [JsonProperty("details")]
        public string? Details { get; set; }

        [JsonProperty("length")]
        public long? Length { get; set; }

        [JsonProperty("placeholder")]
        public bool Placeholder { get; set; }

        [JsonProperty("has_odds")]
        public bool HasOdds { get; set; }

        [JsonProperty("has_premium_odds")]
        public bool HasPremiumOdds { get; set; }
        [JsonProperty("starting_at")]
        public DateTime? StartingAt { get; set; }

        [JsonProperty("starting_at_timestamp")]
        public double StartingAtTimestamp { get; set; }

        [JsonProperty("participants")] // if add to url "include=participants"
        public List<Participant> Participants { get; set; }

        [JsonProperty("league")]
        public League League { get; set; }

        [JsonProperty("sport")]
        public Sport Sport { get; set; }

        [JsonProperty("round")]
        public Round Round { get; set; }

        [JsonProperty("stage")]
        public Stage Stage { get; set; }

        [JsonProperty("group")]
        public Group Group { get; set; }

        [JsonProperty("aggregate")]
        public Aggregate Aggregate { get; set; }

        [JsonProperty("season")]
        public Season Season { get; set; }

        [JsonProperty("venue")]
        public Venue Venue { get; set; }

        [JsonProperty("state")]
        public State State { get; set; }

        [JsonProperty("weatherReport")]
        public WeatherReport WeatherReport { get; set; }

        [JsonProperty("lineups")]
        public List<Lineup> Lineups { get; set; }

        [JsonProperty("events")]
        public List<Event> Events { get; set; }

        [JsonProperty("timeline")]
        public Event Timeline { get; set; }

        [JsonProperty("comments")]
        public List<News> Comments { get; set; }

        [JsonProperty("postmatchNews")]
        public List<News> PostmatchNews { get; set; }

        [JsonProperty("prematchNews")]
        public List<News> PrematchNews { get; set; }

        [JsonProperty("trends")]
        public List<Trend> Trends { get; set; }

        [JsonProperty("statistics")]
        public List<Statistic> Statistics { get; set; }

        [JsonProperty("periods")]
        public List<Period> Periods { get; set; }

        [JsonProperty("odds")]
        public List<PreMatchOdd> Odds { get; set; }

        //[JsonProperty("premiumOdds")]
        //public List<PremiumOdd> PremiumOdds { get; set; }

        //[JsonProperty("inplayOdds")]
        //public List<InplayOdds> ınplayOdds { get; set; }



        [JsonProperty("tvStations")]
        public List<TvStation> TvStations { get; set; }

        //[JsonProperty("predictions")]
        //public List<Predictions> Predictions { get; set; }

        [JsonProperty("referees")]
        public List<Referee> Referees { get; set; }

        [JsonProperty("formations")]
        public List<Formation> Formations { get; set; }

        //[JsonProperty("ballCoordinates")]
        //public List<BallCoordinate> BallCoordinates { get; set; }

        [JsonProperty("sidelined")]
        public List<Sidelined> Sidelineds { get; set; }

        [JsonProperty("scores")]
        public List<Score> Scores { get; set; }

    }
}
