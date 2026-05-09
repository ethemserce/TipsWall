using System;

namespace PreOddsApi.Core.Model
{
    public class BaseAnalysisEntity : IBaseAnalysisEntity
    {
        public long id { get; set; }
        public DateTime create_date_time { get; set; } = DateTime.Now;
        //public DateTime update_date_time { get; set; } = DateTime.Now;
    }
}
