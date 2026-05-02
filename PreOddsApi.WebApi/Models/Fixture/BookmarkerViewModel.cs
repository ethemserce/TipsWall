using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class bookmakerViewModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<OddViewModel> Odd { get; set; }
    }
}
