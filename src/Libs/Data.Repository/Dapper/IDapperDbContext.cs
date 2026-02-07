using Dapper;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;

namespace Data.Repository.Dapper
{
    public interface IDapperDbContext
    {
        Task<TResult> GetAsync<TResult>(Func<DbConnection, Task<TResult>> taskFunc);

        Task<int> CountAsync(string sql);
        Task<int> CountByTableNameAsync(string tableName);
        Task<T> Find<T>(string sql, Action<SqlBuilder> where = null);
        Task<IEnumerable<T>> ToListAsync<T>(string sql, Action<SqlBuilder> builder = null);
    }
}
