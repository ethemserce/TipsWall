using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class CountryLocaleMap : BaseEntityMap<country_locale>
    {
        public override void Configure(EntityTypeBuilder<country_locale> builder)
        {
            base.Configure(builder);
        }
    }
}
