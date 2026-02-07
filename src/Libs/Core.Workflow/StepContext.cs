using Data.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public class StepContext : StepContextBase<StepContext>
    {
        public string ObjectType { get; set; }
        public string StepName { get; set; }
        public object Input { get; set; }
        public object Output { get; set; }
        public StepResult Result { get; set; }

        public ILogger Logger { get; set; }
        public IServiceProvider ServiceProvider { get; set; }

        public StepContext()
        {
            Result = new StepResult();
        }

        public override void AssignInput(StepContext other)
        {
            Input = other.Input;
            Output = other.Output;
        }
    }
}
