using Dapper;
using Data.Repository.Stability;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Dapper
{
    public class DapperDbContext : IDapperDbContext
    {
        protected IConfiguration configuration;
        protected string connectionString;

        protected string connectionStringName = "PostgresConnection";

        public DapperDbContext(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public DapperDbContext(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public async Task<int> CountByTableNameAsync(string tableName)
        {
            return await Find<int>($@"
                select count(t)
                from ""{tableName}"" as t
            ");
        }

        public async Task<int> CountAsync(string sql)
        {
            return await Find<int>(sql);
        }

        public async Task<T> Find<T>(string sql, Action<SqlBuilder> where = null)
        {
            var sqlBuilder = new SqlBuilder();
            var template = sqlBuilder.AddTemplate(sql);
            where?.Invoke(sqlBuilder);
            return await GetAsync((_) => _.QueryFirstOrDefaultAsync<T>(template.RawSql, template.Parameters));
        }

        public async Task<IEnumerable<T>> ToListAsync<T>(string sql, Action<SqlBuilder> builder = null)
        {
            var sqlBuilder = new SqlBuilder();
            var template = sqlBuilder.AddTemplate(sql);
            builder?.Invoke(sqlBuilder);
            return await GetAsync((_) => _.QueryAsync<T>(template.RawSql, template.Parameters));
        }

        public Task<TResult> GetAsync<TResult>(Func<DbConnection, Task<TResult>> taskFunc)
        {
            var connection = GetConnection();            
            return GetAsync(connection, taskFunc);
        }

        protected virtual string GetConnection()
        {
            return connectionString ?? Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? 
                configuration.GetConnectionString("PostgresConnection");
        }

        private async Task<TResult> GetAsync<TResult>(string connectionString, Func<DbConnection, Task<TResult>> taskFunc)
        {
            return await RetryHelper.
                RetryDbAsAsync(async () =>
                {
                    using (var sqlConnection = new NpgsqlConnection(connectionString))
                    {
                        await sqlConnection.OpenAsync();

                        var result = await taskFunc(sqlConnection);

                        await sqlConnection.CloseAsync();

                        return result;
                    }

                });
        }

    }
}
