using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Tips
{
    public class TipsBaseViewModel
    {
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public bool Success { get; set; }
        public List<TipsViewModel> Tips { get; set; }
    }
}
