using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.Entities.PreOddsEntities
{
    public class squad : BaseEntity
    {
        public long transferId { get; set; }
        public transfer transfer { get; set; }
        public long playerId { get; set; }
        public player player { get; set; }
        public long teamId { get; set; }
        public team team { get; set; }
        public int positionId { get; set; }
        public int detailedPositionId { get; set; }
        public DateOnly start { get; set; }
        public DateOnly end { get; set; }
        public bool captain { get; set; }
        public int jerseyNumber { get; set; }
    }
}
