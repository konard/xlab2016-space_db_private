using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public class SortOperand
    {
        public SortOperator Operator { get; set; }
        public int? Ordinal { get; set; }
    }
}
