using Data.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Workflow
{
    public class StateMachine2<TDbContext, TState> : StateMachineBase<TState, StepContext>
        where TDbContext : DbContext
        where TState : IEquatable<TState>
    {
        private readonly TDbContext db;
        private readonly IServiceProvider serviceProvider;
        private List<StepDefinition> definitions = new List<StepDefinition>();

        public List<StepDefinition> Definitions { get { return definitions; } }

        public StateMachine2(TDbContext db, ILogger logger, IServiceProvider serviceProvider) : base(logger)
        {
            this.db = db;
            this.serviceProvider = serviceProvider;
        }

        public void AddDefinition(StepDefinition definition)
        {
            definition.Step.ObjectType = definition.ObjectType;
            definition.Step.Name = definition.Name;
            definitions.Add(definition);
        }

        public bool AddTransition(TState oldState, TState newState, string objectType, string stepName, 
            Func<Task<bool>> condition = null)
        {
            var definition = definitions.FirstOrDefault(_ => _.ObjectType == objectType && _.Name == stepName);
            if (definition?.Step == null)
                return false;

            return AddTransition(oldState, newState, definition.Step, condition, objectType, stepName);
        }

        public override async Task DoStep(TState oldState, TState newState, StepContext context,
            string objectName = null, string stepName = null)
        {
            var strategy = db.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await db.Database.BeginTransactionAsync();

                try
                {
                    context.Logger = logger;
                    context.ServiceProvider = serviceProvider;
                    await base.DoStep(oldState, newState, context, context.ObjectType, context.StepName);
                }
                catch (TransitionNotFoundException e)
                {
                    context.Result.State = StepState.TransitionNotFound;
                    context.Result.AddError($"Transition from {objectName} {oldState} to {newState} is not defined by state machine");
                    return;
                }

                await transaction.CommitAsync();
            });         
            
            context.Result.State = StepState.Success;
        }
    }
}