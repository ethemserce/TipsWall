using Newtonsoft.Json;
using PreOddsApi.Entities.SportMonks.Football.V3;
using System;
using System.Collections.Generic;

namespace PreOddsApi.Entities.SportMonks.Football.V3
{
    public class ScheduleRound
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("sport_id")]
        public long SportId { get; set; }

        [JsonProperty("league_id")]
        public long LeagueId { get; set; }

        [JsonProperty("season_id")]
        public long SeasonId { get; set; }

        [JsonProperty("stage_id")]
        public long StageId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }

        [JsonProperty("is_current")]
        public bool IsCurrent { get; set; }

        [JsonProperty("starting_at")]
        public DateTimeOffset? StartingAt { get; set; }

        [JsonProperty("ending_at")]
        public DateTimeOffset? EndingAt { get; set; }

        [JsonProperty("games_in_current_week")]
        public bool GamesInCurrentWeek { get; set; }

        [JsonProperty("fixtures")]
        public List<ScheduleFixture> Fixtures { get; set; }
    }
}
