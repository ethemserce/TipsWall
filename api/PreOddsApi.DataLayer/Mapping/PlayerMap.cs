using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class PlayerMap : BaseEntityMap<player>
    {
        public override void Configure(EntityTypeBuilder<player> builder)
        {
            builder.HasMany(o => o.events)
     .WithOne(o => o.player)
     .HasForeignKey(o => o.playerId);

            base.Configure(builder);
        }
    }
}
