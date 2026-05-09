using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class SidelinedMap : BaseEntityMap<sidelined>
    {
        public override void Configure(EntityTypeBuilder<sidelined> builder)
        {
            base.Configure(builder);
        }
    }
}
