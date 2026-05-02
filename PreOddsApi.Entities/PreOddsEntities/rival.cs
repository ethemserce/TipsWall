using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class rival : BaseEntity
    {
        public long sportId { get; set; }
        public sport sport { get; set; }
        public long teamId { get; set; }
        public team team { get; set; }
    }
}
