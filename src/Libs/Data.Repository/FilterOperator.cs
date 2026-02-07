using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository
{
    public enum FilterOperator : int
    {
        Undefined = 0,
        Equals = 1,
        GreaterThan = 2,
        LessThan = 3,
        Like = 4,
        StartsWith = 5,
        EndsWith = 6,
        Between = 7,
        NotEquals = 8,
        Contains = 9,
        GreaterThanOrEqual = 10,
        LessThanOrEqual = 11,
        And = 12,
        Or = 13
    }
}
