using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class BenchMap : BaseEntityMap<bench>
    {
        public override void Configure(EntityTypeBuilder<bench> builder)
        {
            base.Configure(builder);
        }
    }
}
