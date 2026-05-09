using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Fixture
{
    public class RefereeViewModel
    {
        public long Id { get; set; }
        public string CommonName { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string FirstName { get; set; }
        public string FullName { get; set; }
        public string LastName { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
