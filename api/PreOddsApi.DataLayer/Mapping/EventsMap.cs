using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class EventsMap : BaseEntityMap<events>
    {
        public override void Configure(EntityTypeBuilder<events> builder)
        {
            base.Configure(builder);
        }
    }
}
