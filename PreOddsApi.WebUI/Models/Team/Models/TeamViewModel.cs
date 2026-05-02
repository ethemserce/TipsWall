using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebUI.Models.Team.Models
{
    public class TeamViewModel
    {
        public long Id { get; set; }
        public string LogoPath { get; set; }
        public string Name { get; set; }
        public string ShortCode { get; set; }
    }
}
