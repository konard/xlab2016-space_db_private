using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Repository.Workflow
{
    public abstract class TransactionStepContextBase<TStepContext> : StepContextBase<TStepContext>
        where TStepContext : TransactionStepContextBase<TStepContext>
    {
        public TransactionStepContextBase()
        {
        }
    }
}
