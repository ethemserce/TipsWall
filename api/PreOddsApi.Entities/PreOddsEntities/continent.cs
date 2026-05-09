using PreOddsApi.Core.Model;
using System.Collections.Generic;
namespace PreOddsApi.Entities.PreOddsEntities
{
    public class continent : BaseEntity
    {
        public string name { get; set; }
        public string code { get; set; }
        public ICollection<country> countries { get; set; }
    }
}
