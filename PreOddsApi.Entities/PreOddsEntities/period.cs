using PreOddsApi.Core.Model;
using System.Collections.Generic;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class period : BaseEntity
    {
        public long? fixtureId { get; set; }
        public fixture fixture { get; set; }
        public long? typeId { get; set; }
        public types types { get; set; }
        public long? started { get; set; }

        public long? ended { get; set; }

        public int? countsFrom { get; set; }

        public int? actualPeriodStart { get; set; }

        public bool ticking { get; set; }

        public int? sortOrder { get; set; }

        public string description { get; set; }

        public int? timeAdded { get; set; }

        public int? minutes { get; set; }

        public int? seconds { get; set; }
        public ICollection<events> events { get; set; }
    }
}
