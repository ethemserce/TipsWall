using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
namespace PreOddsApi.DataLayer.Mapping
{
    public class RegionMap : BaseEntityMap<region>
    {
        public override void Configure(EntityTypeBuilder<region> builder)
        {
            builder.HasMany(x => x.cities)
                    .WithOne(x => x.region)
                    .HasForeignKey(x => x.regionId);

            base.Configure(builder);
        }
    }
}
