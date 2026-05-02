using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class NewsMap : BaseEntityMap<news>
    {
        public override void Configure(EntityTypeBuilder<news> builder)
        {
            base.Configure(builder);
        }
    }
}
