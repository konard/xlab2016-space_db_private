using Data.Repository.Helpers;
using Data.Repository.Stability;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class DataRepositoryBase<T, TQuery, TFilter, TSort, TKey> : IDataRepository<T, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TFilter : FilterBase<T>
        where TSort : SortBase<T>
        where TQuery : QueryBase<T, TFilter, TSort>
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        private readonly string connectionString;

        public DataRepositoryBase(IConfiguration configuration, ILogger logger)
        {
            this.configuration = configuration;
            this.logger = logger;
            connectionString = ConfigurationHelper.GetConnectionString(configuration);
        }

        public abstract Task<IEnumerable<T>> ByQueryAsync(TQuery query);

        public async Task<TResult> ExecuteAsAsync<TResult>(Func<DbConnection, Task<TResult>> taskFunc)
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                using (var sqlConnection = new NpgsqlConnection(connectionString))
                {
                    await sqlConnection.OpenAsync();
                    var result = await taskFunc(sqlConnection);
                    return result;
                }
            });
        }
    }
}
