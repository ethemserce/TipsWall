using Newtonsoft.Json;
using PreOddsApi.Core.Model;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class city : BaseEntity
    {
        public long regionId { get; set; }
        public virtual region region { get; set; }
        public string name { get; set; }
        public string latitude { get; set; }
        public string longitude { get; set; }
        public ICollection<coach> coaches { get; set; }
    }
}
