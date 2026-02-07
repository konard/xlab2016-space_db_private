using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Stability
{
    public static class RetryHelper
    {
        public static async Task<T> RetryAsAsync<T>(Func<Task<T>> taskFunc, Func<Incremental, RetryPolicy> configure)
        {
            var incremental = Retry
                .WithIncremental(retryCount: 5, initialInterval: TimeSpan.FromSeconds(1), increment: TimeSpan.FromSeconds(2));
            var policy = configure(incremental);

            return await policy
                .ExecuteAction(async () =>
                {
                    return await taskFunc();
                });
        }

        public static T RetryAs<T>(Func<T> taskFunc, Func<Incremental, RetryPolicy> configure)
        {
            var incremental = Retry
                .WithIncremental(retryCount: 5, initialInterval: TimeSpan.FromSeconds(1), increment: TimeSpan.FromSeconds(2));
            var policy = configure(incremental);

            return policy
                .ExecuteAction(() =>
                {
                    return taskFunc();
                });
        }

        public static async Task<T> RetryDbAsAsync<T>(Func<Task<T>> taskFunc)
        {
            return await RetryAsAsync(taskFunc,
                // появляется на реплики, когда master делает vacuum
                (_) => _.Catch<PostgresException>(exception => exception.SqlState == "40001"));
        }

        public static T RetryDbAs<T>(Func<T> taskFunc)
        {
            return RetryAs(taskFunc,
                // появляется на реплики, когда master делает vacuum
                (_) => _.Catch<PostgresException>(exception => exception.SqlState == "40001"));
        }

        public static async Task<T> InvokeDbAsync<T>(Func<Task<T>> taskFunc)
        {
            return await RetryHelper.RetryDbAsAsync(async () =>
            {
                return await taskFunc();
            });
        }
    }
}
