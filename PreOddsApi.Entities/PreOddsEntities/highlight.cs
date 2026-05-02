using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class highlight : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public string location { get; set; }
    }
}
