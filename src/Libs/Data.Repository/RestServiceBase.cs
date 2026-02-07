using Data.Repository.Dapper;
using Data.Repository.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class RestServiceBase<T, TKey> : IRestServiceBase<T, TKey>
        where T : class, IEntityKey<TKey>
    {
        protected readonly ILogger<RestServiceBase<T, TKey>> logger;
        protected readonly IDapperDbContext restDapperDb;
        protected readonly DbContext restDb;
        protected readonly DbSet<T> restSet;
        protected readonly string tableName;
        protected string primaryKeyName = "Id";

        public RestServiceBase(ILogger<RestServiceBase<T, TKey>> logger,
            IDapperDbContext restDapperDb, DbContext restDb,
            string tableName)
        {
            this.logger = logger;
            this.restDapperDb = restDapperDb;
            this.restDb = restDb;
            restSet = restDb.Set<T>();
            this.tableName = tableName;
        }

        public virtual async Task<PagedList<T>> SearchAsync<TFilter, TSort>(QueryBase<T, TFilter, TSort> query)
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>
        {
            return await restDapperDb.SearchPageAsync(tableName, query);
        }

        public virtual async Task<T> FindAsync(TKey id)
        {
            return await restDapperDb.FindWhereColumnValueEqualsAsync<T>(tableName, primaryKeyName, id);
        }

        public virtual async Task<bool> AddAsync(T source)
        {
            if (await restSet.AnyAsync(_ => _.Id.Equals(source.Id)))
                return false;
            
            await restSet.AddAsync(source);

            return true;
        }

        public virtual async Task<bool> UpdateAsync(T source, Action<T, T> patchOrUpdate)
        {
            var original = await restSet.FirstOrDefaultAsync(_ => _.Id.Equals(source.Id));

            if (original == null)
                return false;

            patchOrUpdate?.Invoke(source, original);

            return true;
        }

        public virtual async Task<bool> RemoveAsync(TKey id)
        {
            var original = await restSet.FirstOrDefaultAsync(_ => _.Id.Equals(id));

            if (original == null)
                return false;

            restSet.Remove(original);

            return true;
        }

        public virtual async Task SaveChangesAsync()
        {
            await restDb.SaveChangesAsync();
        }
    }
}
