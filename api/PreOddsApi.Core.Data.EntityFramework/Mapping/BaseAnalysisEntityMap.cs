using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PreOddsApi.Core.Model;

namespace PreOddsApi.Core.Data.EntityFramework.Mapping
{
    public class BaseAnalysisEntityMap<T> : IEntityTypeConfiguration<T> where T : class, IBaseAnalysisEntity
    {
        public virtual void Configure(EntityTypeBuilder<T> builder)
        {
            builder.HasKey(p => p.id);
            builder.Property(p => p.id)
                .ValueGeneratedOnAdd();

            builder.ToTable(typeof(T).Name);
        }
    }
}
