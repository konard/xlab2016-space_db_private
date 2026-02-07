using Newtonsoft.Json;

namespace AI.Interruptions
{
    public static class InterruptionSystem
    {
        public const string TransferAgent = "__transferAgent";
        public const string FinishAgent = "__finishAgent";
        public const string StreamAgent = "__streamAgent";

        public static bool IsInterruptFunction(string functionName)
        {
            var interruptions = new List<string> { TransferAgent, FinishAgent };
            return interruptions.Contains(functionName);
        }

        public static AgentMethodCallResponse ResponseForInterrupt()
        {
            return new AgentMethodCallResponse
            {
                Result = JsonConvert.SerializeObject(new
                {
                    Success = true
                })
            };
        }
    }
}
