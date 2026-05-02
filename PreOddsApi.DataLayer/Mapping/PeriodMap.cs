using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class PeriodMap : BaseEntityMap<period>
    {
        public override void Configure(EntityTypeBuilder<period> builder)
        {
            base.Configure(builder);
        }
    }
}
