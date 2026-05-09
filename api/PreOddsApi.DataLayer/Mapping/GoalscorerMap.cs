using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class GoalscorerMap : BaseEntityMap<goalscorer>
    {
        public override void Configure(EntityTypeBuilder<goalscorer> builder)
        {
            base.Configure(builder);
        }
    }
}
