using Data.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Workflow
{
    public class StepDefinition
    {
        /// <summary>
        /// Тип объекта
        /// </summary>
        public string? ObjectType { get; set; }
        /// <summary>
        /// Наименование
        /// </summary>
        public string? Name { get; set; }
        public StepBase<StepContext> Step { get; set; }
    }
}
