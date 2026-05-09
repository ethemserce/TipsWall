using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class SportMap : BaseEntityMap<sport>
    {
        public override void Configure(EntityTypeBuilder<sport> builder)
        {
            base.Configure(builder);
        }
    }
}
