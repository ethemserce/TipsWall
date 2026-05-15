using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public enum SignalSort
    {
        Confidence = 0,
        Winning = 1,
        Earning = 2,
        Odd = 3,
        Edge = 4,
    }

    public sealed class SignalQuery
    {
        public long? BookmakerId { get; init; }
        // Single-market filter. Kept for backwards compat — callers
        // that want multi-market filtering should use MarketIds.
        public long? MarketId { get; init; }
        // Multi-market filter — preferred by clients with a favourite-
        // markets selection. When non-empty, MarketId is ignored.
        public IReadOnlyList<long>? MarketIds { get; init; }
        public long? LeagueId { get; init; }
        public string? WindowCode { get; init; }
        public int? MatchState { get; init; }
        public decimal? MinRate { get; init; }
        public decimal? MaxRate { get; init; }
        public decimal? MinWinningPercent { get; init; }
        public decimal? MinEarningPercent { get; init; }
        public int? MinSampleCount { get; init; }
        public DateTime? FixtureDate { get; init; }
        // True = keep only outcomes where DSO > İKO ("value bet" definition).
        public bool ValueOnly { get; init; }
        // Cap each fixture to its top-N rows by the active sort. Null = no cap.
        public int? TopPerFixture { get; init; }
        public SignalSort Sort { get; init; } = SignalSort.Confidence;
        // false = descending (default, "best first"), true = ascending.
        public bool SortAscending { get; init; }
        public int Page { get; init; } = 1;
        public int PerPage { get; init; } = 50;
    }

    public sealed class RateQueryResult
    {
        public IReadOnlyList<RateResultDto> Items { get; init; } = Array.Empty<RateResultDto>();
        public RateSummaryDto Summary { get; init; } = new();
        public DateTime? AsOfDate { get; init; }
        public int Total { get; init; }
    }

    public interface IAnalyticsReader
    {
        Task<RateQueryResult> GetSignalsAsync(SignalQuery query, CancellationToken ct = default);
    }
}
