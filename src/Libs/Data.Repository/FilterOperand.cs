using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public class FilterOperand<T>
    {
        public T Operand1 { get; set; }
        public T? Operand2 { get; set; }

        public FilterOperator Operator { get; set; }
    }
}
