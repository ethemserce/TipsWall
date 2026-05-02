using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class TvstationMap : BaseEntityMap<tvstation>
    {
        public override void Configure(EntityTypeBuilder<tvstation> builder)
        {
            base.Configure(builder);
        }
    }
}
