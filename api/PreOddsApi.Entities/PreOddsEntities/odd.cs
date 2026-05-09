using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class odd : BaseEntity
    {
        public long fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long marketId { get; set; }
        public market market { get; set; }
        public long bookmakerId { get; set; }
        public bookmaker bookmaker { get; set; }
        public string label { get; set; }
        public string value { get; set; }
        public string name { get; set; }
        public int? sortOrder { get; set; }
        public string marketDescription { get; set; }
        public string probability { get; set; }
        public string oddGroupProbability { get; set; }
        public string dp3 { get; set; }
        public string fractional { get; set; }
        public string american { get; set; }
        public bool? winning { get; set; }
        public bool? stopped { get; set; }
        public string? total { get; set; }
        public string? handicap { get; set; }
        public string originalLabel { get; set; }
        public int status { get; set; }
        public string participants { get; set; }
        public DateTime? createdAt { get; set; }
        public DateTime latestBookmakerUpdate { get; set; }
    }
}
