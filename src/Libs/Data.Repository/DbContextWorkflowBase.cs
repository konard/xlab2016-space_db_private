using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class DbContextWorkflowBase<TRepository, T, TQuery, TKey, TState, TDto, TDetailedDto, TStepContext> : IWorkflow<T, TDto, TDetailedDto, TQuery, TKey, TState, TStepContext>
        where TRepository : IRepository<T, TQuery, TKey>
        where T : class, IEntityKey<TKey>
        where TQuery : IPaginable
        where TStepContext : StepContextBase<TStepContext>
    {
        protected TRepository repository;

        public TStepContext StepContext { get; set; }

        public DbContextWorkflowBase(TRepository repository)
        {
            this.repository = repository;
        }

        public TRepository GetRepository()
        {
            return repository;
        }

        public void SetRepository(TRepository repository)
        {
            this.repository = repository;
        }

        public abstract Task RunAsync(TState oldState, TState newState, TStepContext stepContext);
    }
}
