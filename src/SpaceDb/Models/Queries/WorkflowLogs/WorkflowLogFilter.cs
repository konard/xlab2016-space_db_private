using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.WorkflowLogs
{
    /// <summary>
    /// Лог workflow
    /// </summary>
    public partial class WorkflowLogFilter : FilterBase<WorkflowLog>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public FilterOperand<long>? Id { get; set; }
        /// <summary>
        /// Ид workflow
        /// </summary>
        public FilterOperand<long>? WorkflowId { get; set; }
        /// <summary>
        /// Сообщение
        /// </summary>
        public FilterOperand<string>? Message { get; set; }
        /// <summary>
        /// Время
        /// </summary>
        public FilterOperand<DateTime>? Time { get; set; }
        /// <summary>
        /// Уровень важности: Info - 1, Warning - 2, Error - 3
        /// </summary>
        public FilterOperand<int>? Severity { get; set; }
    }
}
