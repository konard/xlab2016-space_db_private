using Data.Mapping;
using Data.Repository.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class DbContextRepositoryBase<T, TQuery, TFilter, TSort, TKey, TDto, TMap, TMapOptions> : IRepository<T, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TFilter : FilterBase<T>
        where TSort : SortBase<T>
        where TQuery : QueryBase<T, TFilter, TSort>
        where TMap : MapBase<T, TDto, TMapOptions>
        where TMapOptions : MapOptions, new()
    {
        protected readonly DbContext db;
        protected readonly ILogger<DbContextRepositoryBase<T, TQuery, TFilter, TSort, TKey, TDto, TMap, TMapOptions>> logger;
        protected readonly TMap map;

        public DbContextRepositoryBase(DbContext db,
            ILogger<DbContextRepositoryBase<T, TQuery, TFilter, TSort, TKey, TDto, TMap, TMapOptions>> logger,
            TMap map)
        {
            this.db = db;
            this.logger = logger;
            this.map = map;
        }

        public virtual IQueryable<T> All()
        {
            return db.Set<T>();
        }

        public IQueryable<T> AllInclude()
        {
            return AllProjection(AllInclude(All()));
        }

        public virtual IQueryable<T> ByKey(TKey key)
        {
            return ByKeyProjection(ByKeyInclude(All())).Where(_ => _.Id.Equals(key));
        }

        public virtual async Task<bool> HasKey(TKey key)
        {
            return await All().AnyAsync(_ => _.Id.Equals(key));
        }

        public IQueryable<T> ByQuery(TQuery query)
        {
            var result = AllInclude();
            if (query.Sort != null)
                result = ByQuerySort(result, query.Sort);
            if (query.Filter != null)
                result = ByQueryFilter(result, query.Filter);

            return result;
        }

        public IQueryable<T> ByQueryPaged(TQuery query)
        {
            return RepositoryHelper.ByQueryPaging(ByQuery(query), query.Paging);
        }

        public IQueryable<T> Paged(IQueryable<T> source, TQuery query)
        {
            return RepositoryHelper.ByQueryPaging(source, query.Paging);
        }

        public virtual async Task AddAsync(T source)
        {
            await db.Set<T>().AddAsync(source);
        }

        public virtual bool Update(T source, Action<T, T> mapping = null, Func<IQueryable<T>, IQueryable<T>> include = null,
            Action<T, T> beforeMap = null)
        {
            var dbSet = All().AsQueryable();
            if (include != null)
                dbSet = include(dbSet);
            var original = dbSet.FirstOrDefault(_ => _.Id.Equals(source.Id));
            if (original == null)
                return false;

            beforeMap?.Invoke(source, original);

            map.Map(source, original);
            mapping?.Invoke(source, original);

            return true;
        }

        public virtual async Task<bool> UpdateAsync(T source, Action<T, T> mapping = null, Func<IQueryable<T>, IQueryable<T>> include = null)
        {
            return Update(source, mapping, include);
        }

        public bool Remove(T source)
        {
            var original = All().FirstOrDefault(_ => _.Id.Equals(source.Id));
            if (original == null)
                return false;

            db.Set<T>().Remove(original);

            return true;
        }

        public async Task SaveChangesAsync()
        {
            await db.SaveChangesAsync();
        }

        protected abstract IQueryable<T> AllInclude(IQueryable<T> source);

        protected abstract IQueryable<T> ByKeyInclude(IQueryable<T> source);

        protected virtual IQueryable<T> AllProjection(IQueryable<T> source)
        {
            return source;
        }

        protected virtual IQueryable<T> ByKeyProjection(IQueryable<T> source)
        {
            return source;
        }

        protected virtual IQueryable<T> ByQueryFilter(IQueryable<T> source, TFilter filter)
        {
            if (filter == null)
                return source;

            var filterDescription = QueryHelper.ExpandFilter(filter);

            foreach (var item in filterDescription)
                source = FilterHelper.Filter(source, item.Key, item.Value, null);

            return source;
        }

        protected virtual IQueryable<T> ByQuerySort(IQueryable<T> source, TSort sort)
        {
            if (sort == null)
                return source;

            var sortDescription = QueryHelper.ExpandSort(sort);

            for (var i = 0; i < sortDescription.Count; i++)
            {
                var item = sortDescription[i];
                source = SortHelper.Sort(source, item.Key, item.Value, i > 0);
            }

            return source;
        }

        public DbContext GetDbContext()
        {
            return db;
        }
    }
}
