using PreOddsApi.Core.Model;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class state : BaseEntity
    {
        public string stateCode { get; set; }
        public string name { get; set; }
        public string shortName { get; set; }
        public string developerName { get; set; }
        public ICollection<fixture> fixtures { get; set; }
    }
}
