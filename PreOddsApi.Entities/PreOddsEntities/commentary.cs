using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class commentary : BaseEntity
    {
        public long fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long comment { get; set; }
        public long minute { get; set; }
        public long extra_minute { get; set; }
        public long is_goal { get; set; }
        public string is_important { get; set; }
        public string order { get; set; }
    }
}
