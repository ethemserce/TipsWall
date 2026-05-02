using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class VenueMap : BaseEntityMap<venue>
    {
        public override void Configure(EntityTypeBuilder<venue> builder)
        {
            base.Configure(builder);
        }
    }
}
