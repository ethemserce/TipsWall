using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class RivalMap : BaseEntityMap<rival>
    {
        public override void Configure(EntityTypeBuilder<rival> builder)
        {
            base.Configure(builder);
        }
    }
}
