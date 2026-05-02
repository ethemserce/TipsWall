using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class prd_tips : BaseAnalysisEntity
    {
        public long odd_id { get; set; }
        public long prd_user_id { get; set; }
        public string odd_value { get; set; }
        public int? is_win { get; set; }
    }
}
