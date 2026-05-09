using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Country
{
    public class CountryViewModel
    {
        public long Id { get; set; }
        public string Continent { get; set; }
        //public DateTime CreateDateTime { get; set; }
        //public string Fifa { get; set; }
        public string Logo { get; set; }
        public LogoViewModel LogoSet { get; set; }
        //public string Iso { get; set; }
        //public string Latitude { get; set; }
        //public string Longitude { get; set; }
        public string Name { get; set; }
        public string SubRegion { get; set; }
        //public DateTime UpdateDateTime { get; set; }
        //public string WorldRegion { get; set; }
        //public int Status { get; set; }
        public int Favorite { get; set; }
    }
}
