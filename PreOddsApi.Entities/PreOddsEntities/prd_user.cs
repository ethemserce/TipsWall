using PreOddsApi.Core.Model;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PreOddsApi.Entities.PreOddsEntities
{
    [Table("prd_user")]
    public class prd_user : BaseEntity
    {
        public long id { get; set; }
        public string name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string guid { get; set; }
        public string nick_name { get; set; }
        public string password { get; set; }
        public int user_type { get; set; }
        public string facebook_id { get; set; }
        public string google_id { get; set; }
        public string twitter_id { get; set; }
        public string avatar { get; set; }
        public DateTime create_date_time { get; set; }
    }
}
