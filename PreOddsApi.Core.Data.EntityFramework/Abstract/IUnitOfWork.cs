using System;
using PreOddsApi.Core.Model;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace PreOddsApi.Core.Data.EntityFramework.Abstract
{
    public interface IUnitOfWork<TContext> : IDisposable where TContext : DbContext
    {
        IRepository<TEntity> Repository<TEntity>() where TEntity : class, IBaseEntity, new();
        void SaveChanges();

        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
        bool Commit();
        void Rollback();
    }
}
