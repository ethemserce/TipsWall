using System;

namespace PreOddsApi.WebApi.V3.Dtos
{
    public sealed class FixtureSummaryDto
    {
        public long Id { get; init; }

        public string? Name { get; init; }

        public long LeagueId { get; init; }

        public long? SeasonId { get; init; }

        public long? StageId { get; init; }

        public long? RoundId { get; init; }

        public long? StateId { get; init; }

        public long? VenueId { get; init; }

        public DateTimeOffset? StartingAt { get; init; }

        public bool HasOdds { get; init; }

        public bool HasPremiumOdds { get; init; }

        public int? LengthMinutes { get; init; }

        public string? ResultInfo { get; init; }

        public string? Leg { get; init; }

        public bool Placeholder { get; init; }

        public long? HomeTeamId { get; init; }

        public string? HomeTeamName { get; init; }

        public string? HomeTeamShortCode { get; init; }

        public string? HomeTeamImagePath { get; init; }

        public int? HomeScore { get; init; }

        public long? AwayTeamId { get; init; }

        public string? AwayTeamName { get; init; }

        public string? AwayTeamShortCode { get; init; }

        public string? AwayTeamImagePath { get; init; }

        public int? AwayScore { get; init; }

        public int? LiveMinute { get; init; }

        public int? HomeRedCards { get; init; }

        public int? AwayRedCards { get; init; }

        /// <summary>
        /// True when a VAR event was synced for the home team in the last
        /// 60 seconds — used to flash a "VAR" badge while a review is fresh.
        /// </summary>
        public bool? HomeVarActive { get; init; }

        public bool? AwayVarActive { get; init; }

        /// <summary>
        /// Venue name (stadium) joined from football.venues. Null when
        /// SportMonks didn't ship a venue for the fixture — friendlies
        /// and a few youth leagues fall through.
        /// </summary>
        public string? VenueName { get; init; }

        public string? VenueCity { get; init; }

        /// <summary>
        /// Comma-separated referee names (main + assistants if multiple
        /// were assigned). Pulled from the fixture_referees join; null
        /// when no referee has been published yet.
        /// </summary>
        public string? RefereeName { get; init; }
    }
}
