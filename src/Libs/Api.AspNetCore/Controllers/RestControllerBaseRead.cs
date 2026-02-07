using Data.Mapping;
using Data.Repository;
using Data.Repository.Dapper;
using Data.Repository.Helpers;
using Data.Repository.Stability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Api.AspNetCore.Controllers
{
    public abstract class RestControllerBaseRead<T, TKey, TDto, TQuery, TMap>
        where T : class, IEntityKey<TKey>
        where TQuery : IQueryBase<T>
        where TMap: MapBase2<T, TDto, MapOptions>
        where TDto : class
    {
        protected readonly ILogger<RestControllerBaseRead<T, TKey, TDto, TQuery, TMap>> logger;
        protected readonly IDapperDbContext restDapperDb;
        protected readonly DbContext restDb;
        protected readonly DbSet<T> restSet;
        protected readonly string tableName;
        protected string primaryKeyName = "Id";
        protected readonly TMap map;

        protected RestControllerBaseRead(ILogger<RestControllerBaseRead<T, TKey, TDto, TQuery, TMap>> logger, IDapperDbContext restDapperDb, DbContext restDb, string tableName,
            TMap map)
        {
            this.logger = logger;
            this.restDapperDb = restDapperDb;
            this.restDb = restDb;
            restSet = restDb.Set<T>();
            this.tableName = tableName;
            this.map = map;
        }

        public virtual async Task<PagedList<TDto>> SearchAsync(TQuery query)
        {
            return await SearchUsingDapperAsync(query);
        }

        public virtual async Task<TDto> FindAsync(TKey key)
        {
            return await FindUsingDapperAsync(key);
        }

        protected virtual async Task<PagedList<TDto>> SearchUsingDapperAsync(TQuery query)
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                var list = await FilterHelper.SearchUsingDapperAsync<T, TQuery>(query, restDapperDb, tableName);
                return new PagedList<TDto>(map.Map(list.Result), list.Total, query.Paging);
            });
        }

        protected virtual async Task<PagedList<TDto>> SearchUsingEfAsync<TFilter, TSort>(QueryBase<T, TFilter, TSort> query,
            Func<IQueryable<T>, IQueryable<T>> expression, MapOptions mapOptions = null)
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                var list = await FilterHelper.SearchUsingEfAsync(query, expression, restDb);
                return new PagedList<TDto>(map.Map(list.Result, mapOptions), list.Total, query.Paging);
            });
        }

        protected virtual async Task<TDto> FindUsingDapperAsync(TKey key)
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                var item = await restDapperDb.FindWhereColumnValueEqualsAsync<T>(tableName, primaryKeyName, key);
                return map.Map(item);
            });
        }

        protected virtual async Task<TDto> FindUsingEfAsync(TKey key,
            Func<IQueryable<T>, IQueryable<T>> expression, Func<T, T> postFunc = null)
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                var source = restDb.Set<T>().AsNoTracking();

                if (expression != null)
                    source = expression(source);

                source = source.AsSplitQuery();
                var item = await source.FirstOrDefaultAsync(_ => _.Id.Equals(key));

                if (item == null)
                    return null;

                if (postFunc != null)
                {
                    item = postFunc(item);
                    if (item == null)
                        return null;
                }

                return map.Map(item);
            });
        }
    }
}
