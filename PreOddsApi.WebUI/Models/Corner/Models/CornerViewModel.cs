using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Corner.Models
{
    public class CornerViewModel
    {
        public string Comment { get; set; }
        public int ExtraMinute { get; set; }
        public int Minute { get; set; }
        public long TeamId { get; set; }
        public int Position { get; set; }
    }
}
