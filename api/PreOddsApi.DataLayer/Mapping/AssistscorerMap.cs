using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
namespace PreOddsApi.DataLayer.Mapping
{
    public class AssistscorerMap : BaseEntityMap<assistscorer>
    {
        public override void Configure(EntityTypeBuilder<assistscorer> builder)
        {
            base.Configure(builder);
        }
    }
}
