using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class CoachMap : BaseEntityMap<coach>
    {
        public override void Configure(EntityTypeBuilder<coach> builder)
        {
            base.Configure(builder);
        }
    }
}
