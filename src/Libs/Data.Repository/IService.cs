using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public interface IService<T, TDto, TDetailedDto, TQuery, TKey>
    {
        Task<IEnumerable<TDto>> AllAsync();
        Task<PagedList<TDto>> ByQuery(TQuery query);
        Task<TDetailedDto> ByKey(TKey key);
        Task<bool> HasKey(TKey key);
        Task<T> AddAsync(TDto source);
        bool Update(TDto source);
        Task<bool> UpdateAsync(TDto source);
        bool Remove(TDto source);
        Task<bool> RemoveAsync(TDto source);
        Task SaveChangesAsync();
    }
}
