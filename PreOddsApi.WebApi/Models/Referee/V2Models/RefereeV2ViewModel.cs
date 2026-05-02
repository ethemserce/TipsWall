using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Referee.V2Models
{
    public class RefereeV2ViewModel
    {
        public long Id { get; set; }
        public string CommonName { get; set; }
        public string FirstName { get; set; }
        public string FullName { get; set; }
        public string LastName { get; set; }
    }
}
