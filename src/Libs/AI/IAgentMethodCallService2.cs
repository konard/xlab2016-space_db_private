namespace AI
{
    public interface IAgentMethodCallService2
    {
        Task<AgentMethodCallResponse> Call(AgentMethodCallRequest request);
    }
}
