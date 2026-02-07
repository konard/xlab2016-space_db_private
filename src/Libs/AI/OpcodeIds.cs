using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI
{
    public enum OpcodeIds : int
    {
        Load = 1,
        Save = 2,
        Complete = 3,
        Interrupt = 4,
        StartComplete = 5,
    }
}
