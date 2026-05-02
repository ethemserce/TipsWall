using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Odd.Models
{
    public class BookmarkerForOddsViewModel
    {
        public string Name { get; set; }
        public List<OddViewModel> Odd { get; set; }
    }
}
