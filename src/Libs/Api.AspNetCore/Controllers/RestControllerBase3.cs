using Data.Mapping;
using Data.Repository.Dapper;
using Data.Repository.Helpers;
using Data.Repository.Stability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class RestControllerBase3<T, TKey, TDto, TQuery, TMap> : ControllerBase
        where T : class, IEntityKey<TKey>
        where TQuery: IQueryBase<T>
        where TMap: MapBase2<T, TDto, MapOptions>
        where TDto : class
    {
        protected readonly ILogger<RestServiceBase<T, TKey>> logger;
        protected readonly IDapperDbContext restDapperDb;
        protected readonly DbContext restDb;
        protected readonly DbSet<T> restSet;
        protected readonly string tableName;
        protected string primaryKeyName = "Id";
        protected readonly TMap map;

        protected RestControllerBase3(ILogger<RestServiceBase<T, TKey>> logger, IDapperDbContext restDapperDb, DbContext restDb, string tableName,
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
            var list = await restDapperDb.SearchPageAsync(tableName, query.Paging, query.Filter, query.Sort, query.FilterOperator);
            return new PagedList<TDto>(map.Map(list.Result), list.Total, query.Paging);
        }

        public virtual async Task<TDto> FindAsync(TKey key)
        {
            var item = await restDapperDb.FindWhereColumnValueEqualsAsync<T>(tableName, primaryKeyName, key);
            return map.Map(item);
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
            Func<IQueryable<T>, IQueryable<T>> expression)
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                var list = await FilterHelper.SearchUsingEfAsync(query, expression, restDb);
                return new PagedList<TDto>(map.Map(list.Result), list.Total, query.Paging);
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

        protected virtual async Task AddAsyncCore(T source)
        {
        }

        public virtual async Task<TKey> AddAsync(TDto request)
        {
            var source = map.ReverseMap(request);

            if (await restSet.AnyAsync(_ => _.Id.Equals(source.Id)))
                return default(TKey);

            await restSet.AddAsync(source);

            await AddAsyncCore(source);

            await restDb.SaveChangesAsync();

            return source.Id;
        }

        protected virtual async Task UpdateAsyncCore(T source)
        {
        }

        public virtual async Task<bool> UpdateAsync(TDto request)
        {
            var source = map.ReverseMap(request);

            var original = await restSet.FirstOrDefaultAsync(_ => _.Id.Equals(source.Id));

            if (original == null)
                return false;

            map.Map(source, original);

            await UpdateAsyncCore(original);

            await restDb.SaveChangesAsync();

            return true;
        }

        public virtual async Task<bool> RemoveAsync(TKey key)
        {
            var original = await restSet.FirstOrDefaultAsync(_ => _.Id.Equals(key));

            if (original == null)
                return false;

            restSet.Remove(original);

            await restDb.SaveChangesAsync();

            return true;
        }
    }
}
