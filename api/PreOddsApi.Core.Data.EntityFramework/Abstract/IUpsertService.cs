using Microsoft.EntityFrameworkCore;
using PreOddsApi.Core.Data.EntityFramework.Mapping;
using PreOddsApi.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PreOddsApi.Core.Data.EntityFramework.Abstract
{
    public interface IUpsertService<TContext> where TContext : DbContext
    {
        Task UpsertAsync<T, D>(T value)
            where T : IJsonBaseEntity
            where D : BaseEntity;
        Task UpsertAsync<T, D>(List<T> values)
            where T : IJsonBaseEntity
            where D : BaseEntity;
    }
}
