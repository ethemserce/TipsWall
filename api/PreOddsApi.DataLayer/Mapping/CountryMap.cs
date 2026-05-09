using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class CountryMap : BaseEntityMap<country>
    {
        public override void Configure(EntityTypeBuilder<country> builder)
        {
            builder.HasMany(x => x.regions)
                    .WithOne(x => x.country)
                    .HasForeignKey(x => x.countryId);

            builder.HasMany(x => x.leagues)
                    .WithOne(x => x.country)
                    .HasForeignKey(x => x.countryId);

            base.Configure(builder);
        }
    }
}
