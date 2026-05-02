using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    public class CommentMap : BaseEntityMap<comment>
    {
        public override void Configure(EntityTypeBuilder<comment> builder)
        {
            base.Configure(builder);
        }
    }
}
