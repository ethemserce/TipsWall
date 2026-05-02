using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class PrdTipsMap : BaseAnalysisEntityMap<prd_tips>
    {
        public override void Configure(EntityTypeBuilder<prd_tips> builder)
        {
            base.Configure(builder);
        }
    }
}
