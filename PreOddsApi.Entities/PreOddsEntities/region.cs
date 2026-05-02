using PreOddsApi.Core.Model;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class region : BaseEntity
    {
        public long countryId { get; set; }
        public country country { get; set; }
        public string name { get; set; }
        public ICollection<city> cities { get; set; }
    }
}
