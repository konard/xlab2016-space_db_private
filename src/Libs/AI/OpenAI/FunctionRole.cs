using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.OpenAI
{
    public enum FunctionRole : int
    {
        Unknown = 0,
        PublicMcp = 1,
        UserMcp = 2,
        User = 3,
        Protected = 4,
    }
}
