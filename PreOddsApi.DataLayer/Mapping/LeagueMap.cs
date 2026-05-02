using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class LeagueMap : BaseEntityMap<league>
    {
        public override void Configure(EntityTypeBuilder<league> builder)
        {

            builder.HasMany(x => x.seasons)
                    .WithOne(x => x.league)
                    .HasForeignKey(x => x.leagueId);

            base.Configure(builder);
        }
    }
}
