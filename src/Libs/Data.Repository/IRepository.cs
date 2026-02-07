using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public interface IRepository<T, TQuery, TKey>
        where T : class
    {
        IQueryable<T> All();
        IQueryable<T> AllInclude();
        IQueryable<T> ByQuery(TQuery query);
        IQueryable<T> ByQueryPaged(TQuery query);
        IQueryable<T> Paged(IQueryable<T> source, TQuery query);
        IQueryable<T> ByKey(TKey key);
        Task<bool> HasKey(TKey key);
        Task AddAsync(T source);
        bool Update(T source, Action<T, T> mapping = null, Func<IQueryable<T>, IQueryable<T>> include = null, Action<T, T> beforeMap = null);
        Task<bool> UpdateAsync(T source, Action<T, T> mapping = null, Func<IQueryable<T>, IQueryable<T>> include = null);
        bool Remove(T source);
        Task SaveChangesAsync();
        DbContext GetDbContext();
    }
}
