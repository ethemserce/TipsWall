using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class BookmakerBusinessModel
    {
        public BookmakerBusinessModel()
        {
            this.Odd = new List<OddBusinessModel>();
        }
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string Name { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public List<OddBusinessModel> Odd { get; set; }
    }
}
