using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class StandingDetailMap : BaseEntityMap<standing_detail>
    {
        public override void Configure(EntityTypeBuilder<standing_detail> builder)
        {
            base.Configure(builder);
        }
    }
}
