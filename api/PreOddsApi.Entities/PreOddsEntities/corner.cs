using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class corner : BaseEntity
    {
        public string comment { get; set; }
        public int? extraMinute { get; set; }
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public int? minute { get; set; }
        public long? teamId { get; set; }
        public team team { get; set; }
    }
}
