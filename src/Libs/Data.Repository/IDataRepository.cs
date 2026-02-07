using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public interface IDataRepository<T, TQuery, TKey>
        where T : class
    {
        public Task<IEnumerable<T>> ByQueryAsync(TQuery query);
        public Task<TResult> ExecuteAsAsync<TResult>(Func<DbConnection, Task<TResult>> taskFunc);
    }
}
