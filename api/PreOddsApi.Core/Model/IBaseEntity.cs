
using System;

namespace PreOddsApi.Core.Model
{
    public interface IBaseEntity
    {
        public long id { get; set; }
        public long sportmonks_id { get; set; }
        public DateTime create_date_time { get; set; }
        public DateTime update_date_time { get; set; }
    }
}

