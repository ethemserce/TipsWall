using PreOddsApi.Core.Model;
using PreOddsApi.Entities.SportMonks;
using System.Linq.Expressions;

namespace SportMonks.Football.FootballWorker.Abstract
{
    public interface IInsertService
    {
        Task InsertAsync<T,D>(T value)
            where T : SportMonksBaseEntity
            where D : BaseEntity;
        Task InsertAsync<T,D>(List<T> values)
            where T : SportMonksBaseEntity
            where D : BaseEntity;
    }
}
