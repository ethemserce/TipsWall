using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class CardscorerMap : BaseEntityMap<cardscorer>
    {
        public override void Configure(EntityTypeBuilder<cardscorer> builder)
        {
            base.Configure(builder);
        }
    }
}
