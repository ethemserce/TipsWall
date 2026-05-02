using PreOddsApi.Entities.PreOddsEntities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Data.EntityFramework.Mapping;

namespace PreOddsApi.DataLayer.Mapping
{
    internal class CommentaryMap : BaseEntityMap<commentary>
    {
        public override void Configure(EntityTypeBuilder<commentary> builder)
        {
            base.Configure(builder);
        }
    }
}
