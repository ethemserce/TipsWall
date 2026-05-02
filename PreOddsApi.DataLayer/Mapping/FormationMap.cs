using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    internal class FormationMap : BaseEntityMap<formation>
    {
        public override void Configure(EntityTypeBuilder<formation> builder)
        {
            base.Configure(builder);
        }
    }
}
