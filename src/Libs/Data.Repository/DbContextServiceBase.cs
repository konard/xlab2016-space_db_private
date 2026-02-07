using Data.Mapping;
using Data.Repository.Performance;
using Data.Repository.Stability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public class DbContextServiceBase<TRepository, T, TQuery, TKey, TDto, TDetailedDto, TMap, TDetailedMap, TMapOptions> : IService<T, TDto, TDetailedDto, TQuery, TKey>
        where TRepository : IRepository<T, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TQuery : IPaginable
        where TMap: MapBase<T, TDto, TMapOptions>
        where TDetailedMap : MapBase<T, TDetailedDto, TMapOptions>
        where TMapOptions: MapOptions, new()
    {
        protected readonly TMap map;
        protected readonly TDetailedMap detailedMap;
        protected readonly ILogger logger;

        public TRepository Repository { get; set; }

        public DbContextServiceBase(TRepository repository, TMap map, TDetailedMap detailedMap, ILogger logger)
        {
            Repository = repository;
            this.map = map;
            this.detailedMap = detailedMap;
            this.logger = logger;
        }

        public async Task<IEnumerable<TDto>> AllAsync()
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.AllAsync()", async () =>
            {
                return await RetryHelper.RetryDbAsAsync(async () =>
                {
                    var list = await Repository.AllInclude().ToListAsync();
                    logger.LogInformation($"Count: " + list.Count + ": " + correlationId);
                    return map.Map(list);
                });
                
            }, logger, correlationId);
        }

        public virtual async Task<TDetailedDto> ByKey(TKey key)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.ByKey({key})", async () =>
            {
                return await RetryHelper.RetryDbAsAsync(async () =>
                {
                    var item = await Repository.ByKey(key).FirstOrDefaultAsync();
                    return detailedMap.Map(item);
                });
            }, logger, correlationId);
        }

        public async Task<bool> HasKey(TKey key)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.HasKey({key})", async () =>
            {
                return await RetryHelper.RetryDbAsAsync(async () =>
                {
                    return await Repository.HasKey(key);
                });
            }, logger, correlationId);
        }

        public virtual async Task<List<TDto>> ByQueryMap(IQueryable<T> source)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.ByQueryMap()", async () =>
            {
                return await RetryHelper.RetryDbAsAsync(async () =>
                {
                    var list = await source.ToListAsync();
                    logger.LogInformation($"Count: " + list.Count + ": " + correlationId);
                    return map.Map(list);
                });
            }, logger, correlationId);
        }

        public virtual async Task<PagedList<TDto>> DidByQuery(PagedList<TDto> result, IQueryable<T> source, TQuery query)
        {
            return result;
        }

        public virtual async Task<PagedList<TDto>> ByQuery(TQuery query)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.ByQuery()", async () =>
            {
                return await RetryHelper.RetryDbAsAsync(async () =>
                {
                    if (query.Paging == null)
                        query.Paging = new Paging { Take = 10 };

                    var result = new PagedList<TDto>();
                    var source = Repository.ByQuery(query);
                    if (query?.Paging?.ReturnCount == true)
                    {
                        result.Total = await source.CountAsync();
                        result.Calculate(query.Paging);
                    }
                    query.Paging?.Calculate();
                    source = Repository.Paged(source, query);
                    result.Result = await ByQueryMap(source);
                    return await DidByQuery(result, source, query);
                });
            }, logger, correlationId);            
        }

        public virtual async Task<T> AddAsync(TDto source)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.AddAsync()", async () =>
            {
                var entity = map.ReverseMap(source);
                await Repository.AddAsync(entity);
                return entity;
            }, logger, correlationId);
        }

        public virtual bool Update(TDto source)
        {
            var correlationId = Guid.NewGuid();
            return PerfHelper.RunAsync($"{typeof(T)}.Update()", async () =>
            {
                //var entity = map.ReverseMap(source);
                //return Repository.Update(entity);

                var result = await UpdateAsync(source);
                return result;
            }, logger, correlationId).Result;
        }

        public virtual async Task<bool> UpdateAsync(TDto source)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.UpdateAsync()", async () =>
            {
                var entity = map.ReverseMap(source);
                return await Repository.UpdateAsync(entity);
            }, logger, correlationId);
        }

        public virtual bool Remove(TDto source)
        {
            var correlationId = Guid.NewGuid();
            // TODO: possible thread pool starvation
            return PerfHelper.RunAsync($"{typeof(T)}.Remove()", async () =>
            {
                //var entity = map.ReverseMap(source);
                //return Repository.Remove(entity);

                var result = await RemoveAsync(source);
                return result;
            }, logger, correlationId).Result;
            // TODO: fix!!!
            //var result = RemoveAsync(source).Result;
            //return result;
        }

        public virtual async Task<bool> RemoveAsync(TDto source)
        {
            var correlationId = Guid.NewGuid();
            return await PerfHelper.RunAsync($"{typeof(T)}.RemoveAsync()", async () =>
            {
                var entity = map.ReverseMap(source);
                return Repository.Remove(entity);
            }, logger, correlationId);
        }

        public virtual async Task SaveChangesAsync()
        {
            var correlationId = Guid.NewGuid();
            await PerfHelper.RunAsync($"{typeof(T)}.SaveChangesAsync()", async () =>
            {
                await Repository.SaveChangesAsync();
                return true;
            }, logger, correlationId);
        }
    }
}
