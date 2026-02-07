using Data.Repository.Exceptions;
using Data.Repository.Performance;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public class StateMachineBase<TState, TStepContext>
        where TState : IEquatable<TState>
        where TStepContext : StepContextBase<TStepContext>, new()
    {
        protected readonly ILogger logger;
        protected List<Transition> transitions = new List<Transition>();
        
        public StateMachineBase(ILogger logger)
        {
            this.logger = logger;
        }

        public bool AddTransition(TState oldState, TState newState, StepBase<TStepContext> step, Func<Task<bool>> condition = null,
            string objectName = null, string stepName = null)
        {
            if (string.IsNullOrEmpty(stepName))
            {
                if (transitions.Any(_ => _.OldState.Equals(oldState) && _.NewState.Equals(newState)))
                    return false;
            }
            else
            {
                if (transitions.Any(_ => _.Step.ObjectType == objectName && _.Step.Name == stepName &&
                    _.OldState.Equals(oldState) && _.NewState.Equals(newState)))
                    return false;
            }

            transitions.Add(new Transition { OldState = oldState, NewState = newState, Step = step, Condition = condition });
            return true;
        }

        public virtual async Task DoStep(TState oldState, TState newState, TStepContext context = null,
            string objectName = null, string stepName = null)
        {
            var transition = string.IsNullOrEmpty(objectName) ? 
                transitions.FirstOrDefault(_ => _.OldState.Equals(oldState) && _.NewState.Equals(newState)) :
                transitions.FirstOrDefault(_ => _.Step.ObjectType == objectName &&
                    _.OldState.Equals(oldState) && _.NewState.Equals(newState));
            if (transition == null)
                throw new TransitionNotFoundException($"{oldState} to {newState} transition is not registered");
            if (transition.Condition != null && !await transition.Condition())
                throw new TransitionNotFoundException($"{oldState} to {newState} transition is not allowed");

            var correlationId = Guid.NewGuid();
            await PerfHelper.RunAsync($"{GetType()}.DoStep({oldState}, {newState})", async () =>
            {
                if (context != null)
                    transition.Step.SetContext(context);

                await transition.Step.Run();
                return Task.CompletedTask;
            }, logger, correlationId);
        }

        public class Transition
        {
            public TState OldState { get; set; }
            public TState NewState { get; set; }
            public Func<Task<bool>> Condition { get; set; }
            public StepBase<TStepContext> Step { get; set; }
        }

        public class TransitionNotFoundException : WorkflowException
        {
            public TransitionNotFoundException(string message)
                : base(message)
            {
            }
        }
    }
}
