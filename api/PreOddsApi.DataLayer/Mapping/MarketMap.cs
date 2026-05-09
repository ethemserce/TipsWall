using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class MarketMap : BaseEntityMap<market>
    {
        public override void Configure(EntityTypeBuilder<market> builder)
        {
            base.Configure(builder);
        }
    }
}
