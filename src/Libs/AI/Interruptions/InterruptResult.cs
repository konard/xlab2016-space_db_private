using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AI.Interruptions
{
    public class InterruptResult
    {
        public bool Success {  get; set; }
        public bool CreateNewChat { get; set; }
        public bool ClearText { get; set; }

        public string? ErrorMessage {  get; set; }
    }
}
