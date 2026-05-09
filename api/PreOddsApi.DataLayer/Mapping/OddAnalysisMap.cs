using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
   public class OddAnalysisMap : BaseAnalysisEntityMap<odd_analysis>
    {
        public override void Configure(EntityTypeBuilder<odd_analysis> builder)
        {
            base.Configure(builder);
        }
    }
}
