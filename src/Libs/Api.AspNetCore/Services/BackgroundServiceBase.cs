using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace Api.AspNetCore.Services
{
    public abstract class BackgroundServiceBase<TOptions> : BackgroundService
        where TOptions: BackgroundServiceOptions
    {
        protected readonly int delay;
        protected readonly IServiceScopeFactory serviceScopeFactory;
        protected readonly bool enabled;
        protected readonly string name;
        protected readonly TOptions options;

        public BackgroundServiceBase(IServiceScopeFactory serviceScopeFactory, IConfiguration configuration,
            string name, int delay)
        {
            this.serviceScopeFactory = serviceScopeFactory;
            this.name = name;
            this.delay = delay;
            options = CreateOptions();
            BindOptions("HostedServices:" + name, options, configuration);
            enabled = options.Enabled;
        }

        protected abstract void BindOptions(string name, TOptions options, IConfiguration configuration);

        protected virtual TOptions CreateOptions()
        {
            return (TOptions)new BackgroundServiceOptions();
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            if (!enabled)
                return;
            await Task.Run(() => ProcessTaskAsync(cancellationToken));
        }

        private async Task ProcessTaskAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceScopeFactory.CreateScope())
            {
                await ProcessTaskAsync(cancellationToken, scope);
            }
        }

        protected abstract Task ProcessTaskAsync(CancellationToken cancellationToken, IServiceScope scope);
    }
}
