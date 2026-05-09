using System;
using System.ComponentModel.DataAnnotations;

namespace PreOddsApi.Core.Model
{
    public class BaseEntity : IBaseEntity
    {
        public long id { get; set; }
        [Key]
        public long sportmonks_id { get; set; }
        public DateTime create_date_time { get; set; } = DateTime.Now;
        public DateTime update_date_time { get; set; } = DateTime.Now;
    }
}
    