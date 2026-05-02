using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
   public class PrdUserMap : BaseEntityMap<prd_user>
    {
        public override void Configure(EntityTypeBuilder<prd_user> builder)
        {
            base.Configure(builder);
        }

    }
}
