using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class NewsItemLineMap : BaseEntityMap<newsItemLine>
    {
        public override void Configure(EntityTypeBuilder<newsItemLine> builder)
        {
            base.Configure(builder);
        }
    }
}
