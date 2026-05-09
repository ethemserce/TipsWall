using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class odd_analysis : BaseAnalysisEntity
    {
        public long bookmakerId { get; set; }
        public bookmaker bookmaker { get; set; }
        public long marketId { get; set; }
        public market market { get; set; }
        public string odd_label { get; set; }
        public string odd_total { get; set; }
        public string odd_value { get; set; }
        public string odd_handicap { get; set; }
        public int win_count_1m { get; set; }
        public int lost_count_1m { get; set; }
        public decimal? winning_percent_1m { get; set; }
        public decimal? earning_percent_1m { get; set; }
        public int win_count_3m { get; set; }
        public int lost_count_3m { get; set; }
        public decimal? winning_percent_3m { get; set; }
        public decimal? earning_percent_3m { get; set; }
        public int win_count_6m { get; set; }
        public int lost_count_6m { get; set; }
        public decimal? winning_percent_6m { get; set; }
        public decimal? earning_percent_6m { get; set; }
        public int win_count_1y { get; set; }
        public int lost_count_1y { get; set; }
        public decimal? winning_percent_1y { get; set; }
        public decimal? earning_percent_1y { get; set; }
        public int win_count_all { get; set; }
        public int lost_count_all { get; set; }
        public decimal? winning_percent_all { get; set; }
        public decimal? earning_percent_all { get; set; }
    }
}
