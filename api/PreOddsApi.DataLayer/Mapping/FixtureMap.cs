using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
namespace PreOddsApi.DataLayer.Mapping
{
    public class FixtureMap : BaseEntityMap<fixture>
    {
        public override void Configure(EntityTypeBuilder<fixture> builder)
        {
            builder.HasMany(o => o.odds)
                 .WithOne(o => o.fixture)
                 .HasForeignKey(o => o.fixtureId);

            builder.HasMany(o => o.benches)
                 .WithOne(o => o.fixture)
                 .HasForeignKey(o => o.fixtureId);

            builder.HasMany(o => o.comments)
                .WithOne(o => o.fixture)
                .HasForeignKey(o => o.fixtureId);

            builder.HasMany(o => o.commentaries)
                .WithOne(o => o.fixture)
                .HasForeignKey(o => o.fixtureId);

            builder.HasMany(o => o.corners)
                .WithOne(o => o.fixture)
                .HasForeignKey(o => o.fixtureId);

            builder.HasMany(o => o.events)
                .WithOne(o => o.fixture)
                .HasForeignKey(o => o.fixtureId);

            builder.HasMany(o => o.formations)
                .WithOne(o => o.fixture)
                .HasForeignKey(o => o.fixtureId);

            base.Configure(builder);
        }
    }
}
