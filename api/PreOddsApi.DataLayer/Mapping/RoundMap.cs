using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class RoundMap : BaseEntityMap<round>
    {
        public override void Configure(EntityTypeBuilder<round> builder)
        {
            base.Configure(builder);
        }
    }
}
