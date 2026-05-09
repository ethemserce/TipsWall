using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class ContinentLocaleMap : BaseEntityMap<continent_locale>
    {
        public override void Configure(EntityTypeBuilder<continent_locale> builder)
        {
            base.Configure(builder);
        }
    }
}
