using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class continent_locale : BaseEntity
    {
        public long continentId { get; set; }
        public continent  continent { get; set; }
        public string locale { get; set; }
        public string name { get; set; }
    }
}
