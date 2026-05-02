using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class MarketBusinessModel
    {
        public MarketBusinessModel()
        {
            this.Bookmakers = new List<BookmakerBusinessModel>();
        }
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public int Flag { get; set; }
        public List<BookmakerBusinessModel> Bookmakers { get; set; }
    }
}
