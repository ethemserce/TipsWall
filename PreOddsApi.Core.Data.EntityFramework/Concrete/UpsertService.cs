using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PreOddsApi.Core.Data.EntityFramework.Abstract;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Core.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreOddsApi.Core.Data.EntityFramework.Concrete
{
    public class UpsertService<TContext> : IUpsertService<TContext>
    where TContext : DbContext
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public UpsertService(IServiceScopeFactory serviceScopeFactory, IMapper mapper)
        {
            _scopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _mapper = mapper ?? throw new ArgumentNullException();
        }

        public async Task UpsertAsync<T, D>(T value)
          where T : IJsonBaseEntity
          where D : BaseEntity // **D'nin bir class olmasını garanti ediyoruz**
        {
            if (value == null) return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

                // ✅ DbContext'in gerçekten bu entity'yi desteklediğinden emin olalım
                var dbSet = dbContext.Set<D>();
                if (dbSet == null)
                    throw new InvalidOperationException($"DbSet<{typeof(D).Name}> not found in {typeof(TContext).Name}");

                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                try
                {
                    var dbItem = await dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.id == value.Id);

                    if (dbItem == null)
                    {
                        var newEntity = _mapper.Map<D>(value);
                        await dbSet.AddAsync(newEntity);
                    }
                    else
                    {
                        var updatedEntity = _mapper.Map<D>(value);
                        dbContext.Entry(dbItem).CurrentValues.SetValues(updatedEntity);
                    }

                    await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception exc)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("InsertAsync failed", exc);
                }
            }
        }

        public async Task UpsertAsync<T, D>(List<T> values)
         where T : IJsonBaseEntity
         where D : BaseEntity // **D'nin bir class olmasını garanti ediyoruz**
        {
            if (values == null || values.Count == 0) return;

            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TContext>();

                // ✅ DbContext'in gerçekten bu entity'yi desteklediğinden emin olalım
                var dbSet = dbContext.Set<D>();
                if (dbSet == null)
                    throw new InvalidOperationException($"DbSet<{typeof(D).Name}> not found in {typeof(TContext).Name}");

                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                try
                {
                    var trackedEntities = new Dictionary<string, object>();
                    var dbValues = _mapper.Map<List<D>>(values);
                    await UpsertRecursive(dbValues, dbContext);
                    await dbContext.SaveChangesAsync();
                    trackedEntities.Clear();

                    //var dbItem = await dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.id == value.Id);

                    //if (dbItem == null)
                    //{
                    //    var newEntity = _mapper.Map<D>(value);
                    //    await dbSet.AddAsync(newEntity);
                    //}
                    //else
                    //{
                    //    var updatedEntity = _mapper.Map<D>(value);
                    //    dbContext.Entry(dbItem).CurrentValues.SetValues(updatedEntity);
                    //}

                    //await dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception exc)
                {
                    await transaction.RollbackAsync();
                    throw new Exception("InsertAsync failed", exc);
                }
            }
        }

        public async Task UpsertRecursive<T>(List<T> entities, DbContext context, object parentKey = null)
            where T : BaseEntity
        {
            var entityType = context.Model.FindEntityType(typeof(T));
            var primaryKey = entityType.FindPrimaryKey().Properties.FirstOrDefault();
            var dbSet = context.Set<T>();

            foreach (var entity in entities)
            {
                var primaryKeyValue = primaryKey.PropertyInfo.GetValue(entity);

                // ✅ **Önce parent nesneleri kontrol et ve recursive olarak ekle**
                var parentProperties = typeof(T).GetProperties()
                    .Where(p => typeof(BaseEntity).IsAssignableFrom(p.PropertyType)) // Parent olan property'leri al
                    .ToList();

                foreach (var parentProperty in parentProperties)
                {
                    var parentEntity = parentProperty.GetValue(entity);
                    if (parentEntity != null)
                    {
                        var parentType = parentProperty.PropertyType;
                        var method = this.GetType().GetMethod(nameof(UpsertRecursive))
                            .MakeGenericMethod(parentType);

                        var parentList = Activator.CreateInstance(typeof(List<>).MakeGenericType(parentType)) as IList;
                        parentList.Add(Convert.ChangeType(parentEntity, parentType));

                        await (Task)method.Invoke(this, new object[] { parentList, context, primaryKeyValue });
                    }
                }

                // ✅ **1️⃣ Veritabanında kayıt var mı kontrol et?**
                var existingEntity = dbSet.AsNoTracking()
                    .AsEnumerable() // 👈 **LINQ çeviri hatasını çözüyor**
                    .FirstOrDefault(e => primaryKey.PropertyInfo.GetValue(e).Equals(primaryKeyValue));

                if (existingEntity == null)
                {
                    // 🛠 **Sadece ana nesneyi ekle, child nesneler hariç**
                    entity.create_date_time = DateTime.UtcNow;
                    context.Entry(entity).State = EntityState.Added;
                    await context.SaveChangesAsync(); // Parent'ı kaydet, sonra child'lar eklenebilir
                }
                else
                {
                    // **Takip edilen entity’yi çıkar**
                    var trackedEntity = context.ChangeTracker.Entries<T>()
                        .FirstOrDefault(e => primaryKey.PropertyInfo.GetValue(e.Entity).Equals(primaryKeyValue));

                    if (trackedEntity != null)
                    {
                        context.Entry(trackedEntity.Entity).State = EntityState.Detached;
                    }

                    // **Veriyi güncelle**
                    //entity.id = existingEntity.id;
                    entity.create_date_time = existingEntity.create_date_time;
                    entity.update_date_time = DateTime.UtcNow;

                    context.Entry(entity).State = EntityState.Modified;
                    await context.SaveChangesAsync(); // Parent güncellendi
                }

                // ✅ **2️⃣ Eğer Parent ID varsa, ilgili child kayıtlara set et**
                if (parentKey != null)
                {
                    var parentProperty = typeof(T).GetProperties()
                        .FirstOrDefault(p => p.PropertyType == parentKey.GetType());

                    if (parentProperty != null)
                    {
                        parentProperty.SetValue(entity, parentKey);
                    }
                }

                // ✅ **3️⃣ İç içe koleksiyonları işle**
                var collectionProperties = typeof(T).GetProperties()
                    .Where(p => typeof(IEnumerable<object>).IsAssignableFrom(p.PropertyType) && p.PropertyType != typeof(string))
                    .ToList();

                foreach (var collectionProperty in collectionProperties)
                {
                    var childEntities = collectionProperty.GetValue(entity);
                    if (childEntities != null)
                    {
                        var childType = collectionProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                        if (childType == null) continue;

                        var method = this.GetType().GetMethod(nameof(UpsertRecursive))
                            .MakeGenericMethod(childType);

                        // 🛠 **HATA ÇÖZÜMÜ: Doğru tipe cast et**
                        var childList = Activator.CreateInstance(typeof(List<>).MakeGenericType(childType)) as IList;
                        foreach (var item in (IEnumerable<object>)childEntities)
                        {
                            childList.Add(Convert.ChangeType(item, childType));
                        }

                        // ✅ **Veritabanı güncellenmeden çocuk nesneler kaydedilmesin**
                        var foreignKeyProperty = childType.GetProperties().FirstOrDefault(p => p.PropertyType == primaryKey.PropertyInfo.PropertyType);

                        if (foreignKeyProperty != null)
                        {
                            await (Task)method.Invoke(this, new object[] { childList, context, primaryKeyValue });
                        }

                    }
                }
            }
        }

        private void ResolveNavigationProperties<T>(
    T entity,
    DbContext context,
    Dictionary<string, object> trackedEntities)
        {
            // Get all navigation properties of the entity
            var navigationProperties = context.Entry(entity)
                .Metadata
                .GetNavigations()
                .Where(n => !n.IsCollection) // Ignore collections for simplicity
                .ToList();

            foreach (var navigationProperty in navigationProperties)
            {
                var navigationValue = context.Entry(entity).Reference(navigationProperty.Name).CurrentValue;

                if (navigationValue == null) continue;

                // Generate a unique key for the tracked entity cache (e.g., EntityType|Id)
                var entityType = navigationValue.GetType();
                var keyProperties = context.Model.FindEntityType(entityType).FindPrimaryKey().Properties;
                var keyValues = keyProperties.Select(p => entityType.GetProperty(p.Name).GetValue(navigationValue)?.ToString() ?? "").ToArray();
                var cacheKey = $"{entityType.Name}|{string.Join("|", keyValues)}";

                if (trackedEntities.TryGetValue(cacheKey, out var existingEntity))
                {
                    // Use the tracked entity if already exists
                    context.Entry(entity).Reference(navigationProperty.Name).CurrentValue = existingEntity;
                }
                else
                {
                    // Attach the navigation entity and add it to the trackedEntities cache
                    context.Attach(navigationValue);
                    trackedEntities[cacheKey] = navigationValue;
                }
            }
        }

    }
}
