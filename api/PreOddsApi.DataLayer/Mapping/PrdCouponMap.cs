using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class PrdCouponMap : BaseEntityMap<prd_coupon>
    {
        public override void Configure(EntityTypeBuilder<prd_coupon> builder)
        {
            base.Configure(builder);
        }
    }
}
