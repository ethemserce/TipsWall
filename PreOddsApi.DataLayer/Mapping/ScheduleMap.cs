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
    public class ScheduleMap : BaseEntityMap<schedule>
    {
        public override void Configure(EntityTypeBuilder<schedule> builder)
        {
            base.Configure(builder);
        }
    }
}
