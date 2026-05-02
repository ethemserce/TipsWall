using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Model;

namespace PreOddsApi.Core.Data.EntityFramework.Mapping
{
    public class BaseEntityMap<T> : IEntityTypeConfiguration<T> where T : class, IBaseEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(p => p.sportmonks_id);
            builder.Property(p => p.sportmonks_id)
                .ValueGeneratedOnAdd();
            
            
           builder.ToTable(typeof(T).Name);
        }
    }
}