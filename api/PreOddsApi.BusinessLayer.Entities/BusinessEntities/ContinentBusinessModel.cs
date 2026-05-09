using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class ContinentBusinessModel : BaseBusinessModel
    {
        public ContinentBusinessModel()
        {
            this.Countries = new List<CountryBusinessModel>();
        }

        public string Name { get; set; }
        public string Code { get; set; }

        public List<CountryBusinessModel> Countries { get; set; }
    }
}
