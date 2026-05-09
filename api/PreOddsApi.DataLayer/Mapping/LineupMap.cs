using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class LineupMap : BaseEntityMap<lineup>
    {
        public override void Configure(EntityTypeBuilder<lineup> builder)
        {
            base.Configure(builder);
        }
    }
}
