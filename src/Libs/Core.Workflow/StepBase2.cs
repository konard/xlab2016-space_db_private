using Core.Workflow;
using Data.Repository;

namespace Core.Workflow
{
    public abstract class StepBase2<TInput> : StepBase<StepContext>
        where TInput : class
    {        
        protected TInput input;

        public override void SetContext(StepContext stepContext)
        {
            base.SetContext(stepContext);
            input = (TInput)stepContext.Input;
        }
    }

    public abstract class StepBase2<TInput, TOutput> : StepBase<StepContext>
        where TInput : class
        where TOutput : class
    {
        protected TInput input;
        protected TOutput output;

        public override void SetContext(StepContext stepContext)
        {
            base.SetContext(stepContext);
            input = (TInput)stepContext.Input;
            output = (TOutput)stepContext.Output;
        }
    }
}
