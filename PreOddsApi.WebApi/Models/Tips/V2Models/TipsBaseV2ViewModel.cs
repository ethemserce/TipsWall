using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Tips.V2Models
{
    public class TipsBaseV2ViewModel
    {
        public TipsBaseV2ViewModel()
        {
            this.Tips = new List<TipsV2ViewModel>();
        }
        public int Page { get; set; }
        public bool IsLastPage { get; set; }
        public bool Success { get; set; }
        public List<TipsV2ViewModel> Tips { get; set; }
    }
}
