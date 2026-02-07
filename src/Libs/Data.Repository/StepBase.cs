using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository
{
    public abstract class StepBase<TStepContext>
        where TStepContext : StepContextBase<TStepContext>, new()
    {
        protected TStepContext stepContext;

        public string ObjectType { get; set; }
        public string Name { get; set; }

        public StepBase()
        {
            stepContext = new TStepContext();
        }

        public StepBase(TStepContext stepContext)
        {
            this.stepContext = stepContext;
        }

        public virtual void SetContext(TStepContext stepContext)
        {
            this.stepContext = stepContext;
        }

        public abstract Task Run();
    }
}
