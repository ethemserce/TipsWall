using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class TrendMap : BaseEntityMap<trend>
    {
        public override void Configure(EntityTypeBuilder<trend> builder)
        {
            base.Configure(builder);
        }
    }
}
