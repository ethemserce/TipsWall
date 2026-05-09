using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class CornerMap : BaseEntityMap<corner>
    {
        public override void Configure(EntityTypeBuilder<corner> builder)
        {
            base.Configure(builder);
        }
    }
}
