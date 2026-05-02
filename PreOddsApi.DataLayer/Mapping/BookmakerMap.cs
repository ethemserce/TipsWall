using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class BookmakerMap : BaseEntityMap<bookmaker>
    {
        public override void Configure(EntityTypeBuilder<bookmaker> builder)
        {
            base.Configure(builder);
        }
    }
}
