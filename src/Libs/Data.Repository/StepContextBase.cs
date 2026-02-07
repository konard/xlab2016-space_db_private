using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public abstract class StepContextBase<TStepContext>
        where TStepContext : StepContextBase<TStepContext>
    {
        public abstract void AssignInput(TStepContext other);
    }
}
