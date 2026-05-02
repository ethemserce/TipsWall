using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Entities.PreOddsEntities;

namespace PreOddsApi.DataLayer.Mapping
{
    public class TypesMap : BaseEntityMap<types>
    {
        public override void Configure(EntityTypeBuilder<types> builder)
        {
            base.Configure(builder);
        }
    }
}
