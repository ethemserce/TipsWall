using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.WebApi.Models.Comment.V2Models
{
    public class CommentV2ViewModel
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public int ExtraMinute { get; set; }
        public long FixtureId { get; set; }
        public int Goal { get; set; }
        public int Important { get; set; }
        public int Minute { get; set; }
        public int Order { get; set; }
    }
}
