using Data.Repository;
using SpaceDb.Data.SpaceDb.Entities;

namespace SpaceDb.Models.Queries.WorkflowLogs
{
    /// <summary>
    /// Лог workflow
    /// </summary>
    public partial class WorkflowLogSort : SortBase<WorkflowLog>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public SortOperand? Id { get; set; }
        /// <summary>
        /// Ид workflow
        /// </summary>
        public SortOperand? WorkflowId { get; set; }
        /// <summary>
        /// Сообщение
        /// </summary>
        public SortOperand? Message { get; set; }
        /// <summary>
        /// Время
        /// </summary>
        public SortOperand? Time { get; set; }
        /// <summary>
        /// Уровень важности: Info - 1, Warning - 2, Error - 3
        /// </summary>
        public SortOperand? Severity { get; set; }
    }
}
