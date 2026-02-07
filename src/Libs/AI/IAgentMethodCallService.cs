namespace AI
{
    public interface IAgentMethodCallService
    {
        Task<AgentMethodCallResponse> Call(AgentMethodCallRequest request);
    }
}
