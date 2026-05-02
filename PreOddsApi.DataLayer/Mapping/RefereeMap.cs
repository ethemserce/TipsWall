using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class RefereeMap : BaseEntityMap<referee>
    {
        public override void Configure(EntityTypeBuilder<referee> builder)
        {
            base.Configure(builder);
        }
    }
}
