using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    class SeasonstatsMap : BaseEntityMap<seasonstats>
    {
        public override void Configure(EntityTypeBuilder<seasonstats> builder)
        {
            base.Configure(builder);
        }
    }
}
