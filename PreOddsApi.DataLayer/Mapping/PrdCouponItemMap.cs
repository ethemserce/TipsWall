using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class PrdCouponItemMap : BaseEntityMap<prd_coupon_item>
    {
        public override void Configure(EntityTypeBuilder<prd_coupon_item> builder)
        {
            base.Configure(builder);
        }
    }
}
