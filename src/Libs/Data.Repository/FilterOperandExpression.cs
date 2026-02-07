using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public class FilterOperandExpression
    {
        public FilterOperator Operator { get; set; }
        public object Operand1 { get; set; }
        public object Operand2 { get; set; }
        public Type Type { get; set; }
    }
}
