using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class StateMap : BaseEntityMap<state>
    {
        public override void Configure(EntityTypeBuilder<state> builder)
        {
            base.Configure(builder);
        }
    }
}
