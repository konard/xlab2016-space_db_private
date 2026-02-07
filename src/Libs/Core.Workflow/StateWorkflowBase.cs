using Core.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public abstract class StateWorkflowBase<TDbContext, TState>
        where TDbContext : DbContext
        where TState : IEquatable<TState>
    {
        protected readonly TDbContext db;
        protected readonly ILogger logger;
        protected StateMachine2<TDbContext, TState> machine;
        private bool isMachineConfigured;

        public StateWorkflowBase(TDbContext db, ILogger logger, IServiceProvider serviceProvider)
        {
            this.db = db;
            this.logger = logger;
            machine = new StateMachine2<TDbContext, TState>(db, logger, serviceProvider);
        }

        public abstract Task<bool> ConfigureMachine(StepContext stepContext);

        public async Task RunAsync(TState oldState, TState newState, StepContext stepContext)
        {
            if (!isMachineConfigured)
            {
                if (!await ConfigureMachine(stepContext))
                    return;
                isMachineConfigured = true;
            }

            await machine.DoStep(oldState, newState, stepContext);
        }
    }
}
