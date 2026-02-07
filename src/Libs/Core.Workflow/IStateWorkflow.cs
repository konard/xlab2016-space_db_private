using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public interface IStateWorkflow<TState>
        where TState : IEquatable<TState>
    {
        Task RunAsync(TState oldState, TState newState, StepContext stepContext);
    }
}
