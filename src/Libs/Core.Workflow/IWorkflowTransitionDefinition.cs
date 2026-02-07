using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public interface IWorkflowTransitionDefinition<TState>
        where TState : IEquatable<TState>
    {
        /// <summary>
        /// Тип объекта к которому применяется StateMachine
        /// </summary>
        public string? ObjectType { get; set; }
        /// <summary>
        /// Предыдущий статус
        /// </summary>
        public TState PreviousState { get; set; }
        /// <summary>
        /// Следующий статус
        /// </summary>
        public TState NextState { get; set; }
        /// <summary>
        /// Шаг выполняемый при переходе
        /// </summary>
        public string? StepName { get; set; }
    }
}
