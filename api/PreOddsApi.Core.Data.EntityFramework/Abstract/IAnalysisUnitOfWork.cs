using System;
using PreOddsApi.Core.Model;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace PreOddsApi.Core.Data.EntityFramework.Abstract
{
    public interface IAnalysisUnitOfWork<TContext> : IDisposable where TContext : DbContext
    {
        IAnalysisRepository<TEntity> Repository<TEntity>() where TEntity : class, IBaseAnalysisEntity, new();
        void SaveChanges();

        void BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Unspecified);
        bool Commit();
        void Rollback();
    }
}
