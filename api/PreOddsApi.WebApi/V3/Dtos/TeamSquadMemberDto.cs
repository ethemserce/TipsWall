using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    /// <summary>
    /// A single roster row joining team_squads × players for the current
    /// or supplied season. Carries enough for the squad list to render
    /// (jersey + name + age + position chip + captain flag) without a
    /// follow-up per-player fetch.
    /// </summary>
    public sealed class TeamSquadMemberDto
    {
        public long PlayerId { get; init; }
        public long? SeasonId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string? DisplayName { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? ImagePath { get; init; }
        public DateTime? DateOfBirth { get; init; }
        public long? NationalityId { get; init; }
        public int? Height { get; init; }
        public int? Weight { get; init; }

        // Squad-level fields (the same player can have different
        // position/jersey across seasons).
        public int? JerseyNumber { get; init; }
        public bool? Captain { get; init; }
        public long? PositionId { get; init; }
        public string? PositionCode { get; init; }
    }
}
