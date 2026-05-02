using PreOddsApi.Core.Model;
using System;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class market : BaseEntity
    {
        public long? legacy_id { get; set; }
        public string name { get; set; }
        public string developer_name { get; set; }
        public bool has_winning_calculations { get; set; }
        public int flag { get; set; } = 0;
    }
}
