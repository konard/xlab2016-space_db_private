using Microsoft.EntityFrameworkCore;
using SpaceDb.Data.SpaceDb.DatabaseContext;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Services
{
    /// <summary>
    /// Service for logging workflow processes to database
    /// Provides convenient methods for tracking long-running operations
    /// </summary>
    public class WorkflowLogService : IWorkflowLogService
    {
        private readonly SpaceDbContext _dbContext;
        private readonly ILogger<WorkflowLogService> _logger;

        public WorkflowLogService(
            SpaceDbContext dbContext,
            ILogger<WorkflowLogService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<long> StartWorkflowAsync(string workflowName, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
            {
                throw new ArgumentException("Workflow name cannot be empty", nameof(workflowName));
            }

            // Generate new workflow ID (simple incrementing ID for now)
            var maxWorkflowId = await _dbContext.WorkflowLogs!
                .Select(w => (long?)w.WorkflowId)
                .MaxAsync() ?? 0;

            var workflowId = maxWorkflowId + 1;

            var startMessage = string.IsNullOrWhiteSpace(description)
                ? $"Workflow '{workflowName}' started"
                : $"Workflow '{workflowName}' started: {description}";

            await LogInfoAsync(workflowId, startMessage);

            _logger.LogInformation("Started workflow {WorkflowId}: {WorkflowName}", workflowId, workflowName);

            return workflowId;
        }

        /// <inheritdoc/>
        public async Task LogInfoAsync(long workflowId, string message)
        {
            await AddLogAsync(workflowId, message, WorkflowLogSeverity.Info);
        }

        /// <inheritdoc/>
        public async Task LogWarningAsync(long workflowId, string message)
        {
            await AddLogAsync(workflowId, message, WorkflowLogSeverity.Warning);
            _logger.LogWarning("Workflow {WorkflowId}: {Message}", workflowId, message);
        }

        /// <inheritdoc/>
        public async Task LogErrorAsync(long workflowId, string message, Exception? exception = null)
        {
            var fullMessage = exception != null
                ? $"{message} | Exception: {exception.GetType().Name}: {exception.Message}"
                : message;

            await AddLogAsync(workflowId, fullMessage, WorkflowLogSeverity.Error);

            if (exception != null)
            {
                _logger.LogError(exception, "Workflow {WorkflowId}: {Message}", workflowId, message);
            }
            else
            {
                _logger.LogError("Workflow {WorkflowId}: {Message}", workflowId, message);
            }
        }

        /// <inheritdoc/>
        public async Task CompleteWorkflowAsync(long workflowId, string? message = null)
        {
            var completionMessage = string.IsNullOrWhiteSpace(message)
                ? "Workflow completed successfully"
                : $"Workflow completed: {message}";

            await LogInfoAsync(workflowId, completionMessage);
            _logger.LogInformation("Completed workflow {WorkflowId}", workflowId);
        }

        /// <inheritdoc/>
        public async Task FailWorkflowAsync(long workflowId, string message, Exception? exception = null)
        {
            var failureMessage = $"Workflow failed: {message}";
            await LogErrorAsync(workflowId, failureMessage, exception);
            _logger.LogError("Failed workflow {WorkflowId}: {Message}", workflowId, message);
        }

        /// <inheritdoc/>
        public async Task<List<WorkflowLogEntry>> GetWorkflowLogsAsync(long workflowId)
        {
            var logs = await _dbContext.WorkflowLogs!
                .Where(l => l.WorkflowId == workflowId)
                .OrderBy(l => l.Time)
                .Select(l => new WorkflowLogEntry
                {
                    Id = l.Id,
                    WorkflowId = l.WorkflowId,
                    Message = l.Message,
                    Time = l.Time,
                    Severity = (WorkflowLogSeverity)l.Severity
                })
                .ToListAsync();

            return logs;
        }

        /// <summary>
        /// Add a log entry to the database
        /// </summary>
        private async Task AddLogAsync(long workflowId, string message, WorkflowLogSeverity severity)
        {
            var logEntry = new WorkflowLog
            {
                WorkflowId = workflowId,
                Message = message,
                Time = DateTime.UtcNow,
                Severity = (int)severity
            };

            _dbContext.WorkflowLogs!.Add(logEntry);
            await _dbContext.SaveChangesAsync();
        }
    }
}
