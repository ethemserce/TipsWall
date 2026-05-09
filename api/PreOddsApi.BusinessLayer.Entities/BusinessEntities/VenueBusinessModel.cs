using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
   public class VenueBusinessModel
    {
        public long Id { get; set; }
        public string Address { get; set; }
        public int Capacity { get; set; }
        public string City { get; set; }
        public string Coordinates { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string ImagePath { get; set; }
        public string Name { get; set; }
        public string Surface { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
