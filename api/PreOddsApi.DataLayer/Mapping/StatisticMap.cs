using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class StatisticMap : BaseEntityMap<statistic>
    {
        public override void Configure(EntityTypeBuilder<statistic> builder)
        {
            base.Configure(builder);
        }
    }
}
