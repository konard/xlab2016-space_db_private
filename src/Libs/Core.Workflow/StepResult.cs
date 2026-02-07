using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public class StepResult
    {
        public bool Success { get { return Errors?.Count == 0; } }
        public StepState State { get; set; }
        public List<string> Errors { get; set; }
        public object Data { get; set; }

        public StepResult()
        {
            Errors = new List<string>();
        }

        public StepResult(StepResult other) : 
            this()
        {
            State = other.State;
            Errors = other.Errors;
        }

        public void AddError(string message)
        {
            Errors.Add(message);
        }

        public StepResult Reject(string message)
        {
            State = StepState.RejectedByStep;
            AddError(message);

            return this;
        }

        public StepResult Ok()
        {
            State = StepState.Success;

            return this;
        }
    }
}
