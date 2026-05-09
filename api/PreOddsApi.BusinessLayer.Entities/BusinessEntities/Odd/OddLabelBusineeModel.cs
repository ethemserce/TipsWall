using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class OddLabelBusineeModel
    {
        public OddLabelBusineeModel()
        {
            this.Odd = new List<OddBusinessModel>();
        }
        public string OddLabel { get; set; }
        public List<OddBusinessModel> Odd { get; set; }
    }
}
