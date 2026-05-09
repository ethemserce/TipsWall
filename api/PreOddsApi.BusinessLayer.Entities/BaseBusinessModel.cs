using System;

namespace PreOddsApi.BusinessLayer.Entities
{
    public class BaseBusinessModel
    {
        public long Id { get; set; }
        public DateTime CreateDateTime { get; set; }
        public DateTime UpdateDateTime { get; set; }
        public int Status { get; set; }
        public int Favorite { get; set; }
    }
}
