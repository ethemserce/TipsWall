using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PreOddsApi.WebApi.V3.Dtos;

namespace PreOddsApi.WebApi.V3.Data
{
    public sealed class RateQuery
    {
        public long? BookmakerId { get; init; }
        public long? MarketId { get; init; }
        public string? WindowCode { get; init; }
        public int? MatchState { get; init; }
        public decimal? MinRate { get; init; }
        public decimal? MinWinningPercent { get; init; }
        public decimal? MinEarningPercent { get; init; }
        public int? MinSampleCount { get; init; }
        public DateTime? FixtureDate { get; init; }
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
        Task<RateQueryResult> GetHotRateAsync(RateQuery query, CancellationToken ct = default);
        Task<RateQueryResult> GetWinningRateAsync(RateQuery query, CancellationToken ct = default);
        Task<RateQueryResult> GetEarningRateAsync(RateQuery query, CancellationToken ct = default);
    }
}
