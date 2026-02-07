namespace AI
{
    public class DownloadAgentFileRequest
    {
        public long? AgentRunId { get; set; }
        public AI.FileInfo? File { get; set; }
    }
}
