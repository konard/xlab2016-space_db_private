using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public interface IWorkflow<T, TDto, TDetailedDto, TQuery, TKey, TState, TStepContext>
        where TStepContext : StepContextBase<TStepContext>
    {
        public TStepContext StepContext { get; set; }

        Task RunAsync(TState oldState, TState newState, TStepContext stepContext);
    }
}
