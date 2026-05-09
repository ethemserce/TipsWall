using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class CoachBusinessModel
    {
        public long Id { get; set; }
        public string BirthCountry { get; set; }
        public string BirthDate { get; set; }
        public string BirthPlace { get; set; }
        public string CommonName { get; set; }
        public DateTime CreateDateTime { get; set; }
        public string FirstName { get; set; }
        public string FullName { get; set; }
        public string ImagePath { get; set; }
        public string LastName { get; set; }
        public string Nationality { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
