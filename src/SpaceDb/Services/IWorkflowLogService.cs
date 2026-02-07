namespace SpaceDb.Services
{
    /// <summary>
    /// Service for logging workflow processes
    /// </summary>
    public interface IWorkflowLogService
    {
        /// <summary>
        /// Start a new workflow and return its ID
        /// </summary>
        /// <param name="workflowName">Name of the workflow</param>
        /// <param name="description">Optional workflow description</param>
        /// <returns>Workflow ID</returns>
        Task<long> StartWorkflowAsync(string workflowName, string? description = null);

        /// <summary>
        /// Log an informational message for a workflow
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="message">Log message</param>
        Task LogInfoAsync(long workflowId, string message);

        /// <summary>
        /// Log a warning message for a workflow
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="message">Log message</param>
        Task LogWarningAsync(long workflowId, string message);

        /// <summary>
        /// Log an error message for a workflow
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="message">Log message</param>
        /// <param name="exception">Optional exception</param>
        Task LogErrorAsync(long workflowId, string message, Exception? exception = null);

        /// <summary>
        /// Complete a workflow successfully
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="message">Completion message</param>
        Task CompleteWorkflowAsync(long workflowId, string? message = null);

        /// <summary>
        /// Fail a workflow with error
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <param name="message">Error message</param>
        /// <param name="exception">Optional exception</param>
        Task FailWorkflowAsync(long workflowId, string message, Exception? exception = null);

        /// <summary>
        /// Get all logs for a specific workflow
        /// </summary>
        /// <param name="workflowId">Workflow ID</param>
        /// <returns>List of workflow logs ordered by time</returns>
        Task<List<WorkflowLogEntry>> GetWorkflowLogsAsync(long workflowId);
    }

    /// <summary>
    /// Workflow log entry
    /// </summary>
    public class WorkflowLogEntry
    {
        public long Id { get; set; }
        public long WorkflowId { get; set; }
        public string? Message { get; set; }
        public DateTime Time { get; set; }
        public WorkflowLogSeverity Severity { get; set; }
    }

    /// <summary>
    /// Workflow log severity levels
    /// </summary>
    public enum WorkflowLogSeverity
    {
        Info = 1,
        Warning = 2,
        Error = 3
    }
}
