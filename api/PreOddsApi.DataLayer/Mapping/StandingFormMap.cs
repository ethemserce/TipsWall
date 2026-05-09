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
    public class StandingFormMap : BaseEntityMap<standing_form>
    {
        public override void Configure(EntityTypeBuilder<standing_form> builder)
        {
            base.Configure(builder);
        }
    }
}
