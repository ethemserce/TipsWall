using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
   public class OddMap : BaseEntityMap<odd>
    {
        public override void Configure(EntityTypeBuilder<odd> builder)
        {
            base.Configure(builder);
        }
    }
}
