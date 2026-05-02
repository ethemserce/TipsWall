using System;
using System.Collections.Generic;
using System.Text;

namespace PreOddsApi.BusinessLayer.Entities.BusinessEntities
{
    public class CommentBusinessModel
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public DateTime CreateDateTime { get; set; }
        public int ExtraMinute { get; set; }
        public long FixtureId { get; set; }
        public int Goal { get; set; }
        public int Important { get; set; }
        public int Minute { get; set; }
        public int Order { get; set; }
        public DateTime UpdateDateTime { get; set; }
    }
}
