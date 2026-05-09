using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
  public  class SeasonMap : BaseEntityMap<season>
    {
        public override void Configure(EntityTypeBuilder<season> builder)
        {
            builder.HasMany(x => x.stages)
                    .WithOne(x => x.season)
                    .HasForeignKey(x => x.seasonId);

            base.Configure(builder);
        }
    }
}
