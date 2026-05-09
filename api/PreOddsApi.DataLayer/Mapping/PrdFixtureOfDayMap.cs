using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class PrdFixtureOfDayMap : BaseEntityMap<prd_fixture_of_day>
    {
        public override void Configure(EntityTypeBuilder<prd_fixture_of_day> builder)
        {
            base.Configure(builder);
        }

    }
}
