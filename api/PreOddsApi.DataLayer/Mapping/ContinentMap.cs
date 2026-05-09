using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class ContinentMap : BaseEntityMap<continent>
    {
        public override void Configure(EntityTypeBuilder<continent> builder)
        {
            builder.HasMany(x => x.countries)
                .WithOne(x => x.continent)
                .HasForeignKey(x => x.continentId);

            base.Configure(builder);
        }
    }
}
