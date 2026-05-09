using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Venue.V2Models
{
    public class VenueV2ViewModel
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int Capacity { get; set; }
        public string City { get; set; }
        public string Coordinates { get; set; }
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string Surface { get; set; }
    }
}
