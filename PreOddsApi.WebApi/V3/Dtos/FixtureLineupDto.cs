using System.Collections.Generic;
using Newtonsoft.Json;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureLineupsDto
    {
        [JsonProperty("home")]
        public FixtureTeamLineupDto? Home { get; init; }

        [JsonProperty("away")]
        public FixtureTeamLineupDto? Away { get; init; }
    }

    public sealed class FixtureTeamLineupDto
    {
        [JsonProperty("team_id")]
        public long? TeamId { get; init; }

        [JsonProperty("formation")]
        public string? Formation { get; init; }

        [JsonProperty("starters")]
        public IReadOnlyList<FixtureLineupPlayerDto> Starters { get; init; }
            = new List<FixtureLineupPlayerDto>();

        [JsonProperty("bench")]
        public IReadOnlyList<FixtureLineupPlayerDto> Bench { get; init; }
            = new List<FixtureLineupPlayerDto>();
    }

    public sealed class FixtureLineupPlayerDto
    {
        [JsonProperty("player_id")]
        public long? PlayerId { get; init; }

        [JsonProperty("player_name")]
        public string? PlayerName { get; init; }

        [JsonProperty("jersey_number")]
        public int? JerseyNumber { get; init; }

        [JsonProperty("formation_field")]
        public string? FormationField { get; init; }

        [JsonProperty("formation_position")]
        public int? FormationPosition { get; init; }

        [JsonProperty("position_code")]
        public string? PositionCode { get; init; }
    }
}
