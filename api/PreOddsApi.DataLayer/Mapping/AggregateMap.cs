using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class AggregateMap : BaseEntityMap<aggregate>
    {
        public override void Configure(EntityTypeBuilder<aggregate> builder)
        {
            base.Configure(builder);
        }
    }
}
