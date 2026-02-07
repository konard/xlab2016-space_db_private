using Api.AspNetCore.Models.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public abstract class DataSyncBackgroundServiceBase<TOptions> : BackgroundServiceBase<TOptions>
        where TOptions : DataSyncBackgroundServiceOptions
    {
        protected ILogger<DataSyncBackgroundServiceBase<TOptions>> logger;

        public DataSyncBackgroundServiceBase(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration, string name, int delay) :
            base(serviceScopeFactory, configuration, name, delay)
        {
        }

        protected abstract Task ResolveServices(IServiceProvider service);

        protected abstract Task ProcessAsync(CancellationToken cancellationToken, IServiceScope scope);

        protected override async Task ProcessTaskAsync(CancellationToken cancellationToken, IServiceScope scope)
        {
            logger = scope.ServiceProvider.GetRequiredService<ILogger<DataSyncBackgroundServiceBase<TOptions>>>();
            await ResolveServices(scope.ServiceProvider);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessAsync(cancellationToken, scope);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception:" + e.Message);
                        logger.LogError(e, "Read error");
                    }

                    await Task.Delay(delay);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Initialization error", ex);
            }
        }

        protected async Task<(TData, T)> Load<TData, TKey, T>(DbContext db,
            string serviceName)
            where TData: class, IBackgroundServiceData<TKey>, new()
            where T : class, new()
        {
            logger.LogInformation($"Fetching sync data");
            var set = db.Set<TData>();
            var data = await set.FirstOrDefaultAsync(_ => _.Name == serviceName);
            T syncData = null;

            if (data == null)
            {
                logger.LogInformation($"Sync data not found, creating");

                syncData = new T();
                data = new TData()
                {
                    Name = serviceName,
                    Data = JsonConvert.SerializeObject(syncData)
                };
                set.Add(data);
                await db.SaveChangesAsync();

                logger.LogInformation($"Empty sync data stored");
            }
            else
            {
                syncData = JsonConvert.DeserializeObject<T>(data.Data);
                logger.LogInformation($"Fetched sync data: " + data.Data);
            }

            return (data, syncData);
        }

        public void Update<TData, TKey, T>(TData data, T syncData,
            Action<T> mutation)
            where TData : class, IBackgroundServiceData<TKey>, new()
            where T : class, new()
        {
            mutation(syncData);
            data.Data = JsonConvert.SerializeObject(syncData);

            logger.LogInformation($"Sync data updated:" + data.Data);
        }
    }
}
