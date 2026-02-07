using AI.Interruptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Helpers
{
    public static class AgentHelper
    {
        public static void AssertShouldCallAs(this AgentMethodCallRequest request, string agentName)
        {
            if (string.IsNullOrEmpty(request.AgentName))
                throw new ArgumentNullException(nameof(request.AgentName));

            if (string.IsNullOrEmpty(request.FunctionName))
                throw new ArgumentNullException(nameof(request.FunctionName));

            if (string.IsNullOrEmpty(request.Args))
                throw new ArgumentNullException(nameof(request.Args));

            if (request.AgentName != agentName)
                throw new InvalidOperationException($"Only {agentName} agent supported");            
        }

        public static T As<T>(this AgentMethodCallRequest request)
        {
            return JsonConvert.DeserializeObject<T>(request.Args);
        }
    }
}
