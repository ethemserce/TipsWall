using AutoMapper;
using Microsoft.EntityFrameworkCore;
using PreOddsApi.Core.Model;
using PreOddsApi.DataLayer;
using PreOddsApi.Entities.SportMonks;
using SportMonks.Football.FootballWorker.Abstract;

namespace SportMonks.Football.FootballWorker.Concrete
{
    public class InsertService : IInsertService
    {
        //private readonly ILogger<InsertService<T> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMapper _mapper;

        public InsertService(ILogger<InsertService> logger,
                IServiceScopeFactory serviceScopeFactory,
                IMapper mapper)
        {
            _scopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            //_logger = logger;
            _mapper = mapper ?? throw new ArgumentNullException();
        }

        public async Task InsertAsync<T, D>(T value)
            where T : SportMonksBaseEntity
            where D : BaseEntity
        {
            if (value != null)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    try
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<PreOddsApiDbContext>();

                        var dbItem = await dbContext.Set<D>().AsNoTracking().FirstOrDefaultAsync(x => x.id == value.Id);

                        if (dbItem == null)
                        {
                            await dbContext.Set<D>().AddAsync(_mapper.Map<D>(value));
                        }
                        else
                        {
                            dbContext.Set<D>().Update(_mapper.Map<D>(value));
                        }

                        await dbContext.SaveChangesAsync();
                    }
                    catch (Exception exc)
                    {

                        throw;
                    }
                }
            }
        }

        public async Task InsertAsync<T, D>(List<T> values)
            where T : SportMonksBaseEntity
            where D : BaseEntity
        {
            if (values != null && values.Count > 0)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<PreOddsApiDbContext>();
                    IList<D> dbList = await dbContext.Set<D>().AsNoTracking().ToListAsync();

                    foreach (var value in values)
                    {
                        var dbItem = dbList.FirstOrDefault(x => x.id == value.Id);

                        if (dbItem == null)
                        {
                            await dbContext.Set<D>().AddAsync(_mapper.Map<D>(value));
                        }
                        else
                        {
                            dbContext.Set<D>().Update(_mapper.Map<D>(value));
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }
}
