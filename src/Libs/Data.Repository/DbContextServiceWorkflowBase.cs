using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class DbContextServiceWorkflowBase<TService, T, TQuery, TKey, TState, TDto, TDetailedDto, TStepContext> : IWorkflow<T, TDto, TDetailedDto, TQuery, TKey, TState, TStepContext>
        where TService : IService<T, TDto, TDetailedDto, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TQuery : IPaginable
        where TStepContext : StepContextBase<TStepContext>
    {
        protected readonly ILogger logger;

        public TStepContext StepContext { get; set; }
        public TService Service { get; set; }
        
        public DbContextServiceWorkflowBase(TService service, ILogger logger)
        {
            Service = service;
            this.logger = logger;
        }

        public abstract Task RunAsync(TState oldState, TState newState, TStepContext stepContext);
    }
}
