using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class OddLabelViewModel
    {
        public OddLabelViewModel()
        {
            this.Odd = new List<OddViewModel>();
        }
        public string OddLabel { get; set; }
        public List<OddViewModel> Odd { get; set; }
    }
}
