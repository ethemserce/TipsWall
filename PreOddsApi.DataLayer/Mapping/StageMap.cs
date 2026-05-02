using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class StageMap : BaseEntityMap<stage>
    {
        public override void Configure(EntityTypeBuilder<stage> builder)
        {
            builder.HasMany(x => x.rounds)
                    .WithOne(x => x.stage)
                    .HasForeignKey(x => x.stageId);

            base.Configure(builder);
        }
    }
}
