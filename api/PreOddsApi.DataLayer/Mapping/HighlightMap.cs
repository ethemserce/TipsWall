using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
   public class HighlightMap : BaseEntityMap<highlight>
    {
        public override void Configure(EntityTypeBuilder<highlight> builder)
        {
            base.Configure(builder);
        }
    }
}
