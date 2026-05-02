using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.DataLayer.Mapping
{
    public class SquadMap : BaseEntityMap<squad>
    {
        public override void Configure(EntityTypeBuilder<squad> builder)
        {
            base.Configure(builder);
        }
    }
}
