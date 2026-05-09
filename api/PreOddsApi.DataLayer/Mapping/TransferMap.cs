using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class TransferMap : BaseEntityMap<transfer>
    {
        public override void Configure(EntityTypeBuilder<transfer> builder)
        {
            base.Configure(builder);
        }
    }
}
