using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class GroupMap : BaseEntityMap<group>
    {
        public override void Configure(EntityTypeBuilder<group> builder)
        {
            base.Configure(builder);
        }
    }
}
