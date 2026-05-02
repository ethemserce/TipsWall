using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class ScoreMap : BaseEntityMap<score>
    {
        public override void Configure(EntityTypeBuilder<score> builder)
        {
            base.Configure(builder);
        }
    }
}
