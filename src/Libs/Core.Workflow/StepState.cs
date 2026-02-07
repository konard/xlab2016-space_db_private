using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public enum StepState : int
    {
        Undefined = 0,
        Success = 1,
        ConfigurationFailed = 2,
        StepNotFound = 3,
        TransitionNotFound = 4,
        AuthorizationFailed = 5,
        RejectedByStep = 6
    }
}
