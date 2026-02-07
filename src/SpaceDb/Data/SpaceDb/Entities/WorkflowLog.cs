using Data.Repository;

namespace SpaceDb.Data.SpaceDb.Entities
{
    /// <summary>
    /// Лог workflow
    /// </summary>
    public partial class WorkflowLog : IEntityKey<long>
    {
        /// <summary>
        /// Ид
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Ид workflow
        /// </summary>
        public long WorkflowId { get; set; }
        /// <summary>
        /// Сообщение
        /// </summary>
        public string? Message { get; set; }
        /// <summary>
        /// Время
        /// </summary>
        public DateTime Time { get; set; }
        /// <summary>
        /// Уровень важности: Info - 1, Warning - 2, Error - 3
        /// </summary>
        public int Severity { get; set; }
    }
}
