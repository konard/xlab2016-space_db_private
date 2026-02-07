using System;
using System.Threading.Tasks;

namespace Data.Repository
{
    public interface IRestServiceBase<T, TKey>
        where T : class
    {
        Task<PagedList<T>> SearchAsync<TFilter, TSort>(QueryBase<T, TFilter, TSort> query)
            where TFilter : FilterBase<T>
            where TSort : SortBase<T>;
        Task<T> FindAsync(TKey id);
        Task<bool> AddAsync(T source);
        Task<bool> UpdateAsync(T source, Action<T, T> patchOrUpdate);
        Task<bool> RemoveAsync(TKey id);
        Task SaveChangesAsync();
    }
}
