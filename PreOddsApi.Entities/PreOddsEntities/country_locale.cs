using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class country_locale : BaseEntity
    {
        public long countryId { get; set; }
        public country country { get; set; }
        public string locale { get; set; }
        public string name { get; set; }
    }
}
