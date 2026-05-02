using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class TopScorerMap : BaseEntityMap<topScorer>
    {
        public override void Configure(EntityTypeBuilder<topScorer> builder)
        {
            base.Configure(builder);
        }
    }
}
