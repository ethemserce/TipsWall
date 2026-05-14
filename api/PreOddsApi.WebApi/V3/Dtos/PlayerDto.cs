using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// Reference card for a single player. Joins football.players to
    /// the player's most-recent team_squads row so the mobile screen
    /// can show the current club + jersey without a second fetch.
    /// </summary>
    public sealed class PlayerDto
    {
        public long Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? ImagePath { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public long? NationalityId { get; init; }
        public long? CountryId { get; init; }
        public int? Height { get; init; }
        public int? Weight { get; init; }
        public long? PositionId { get; init; }
        public string? PositionCode { get; init; }
        public string? Gender { get; init; }

        // Latest team_squads row — null if the player isn't in any
        // active squad we synced.
        public long? CurrentTeamId { get; init; }
        public string? CurrentTeamName { get; init; }
        public string? CurrentTeamImagePath { get; init; }
        public int? CurrentJerseyNumber { get; init; }
        public bool? CurrentCaptain { get; init; }
    }
}
