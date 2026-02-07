using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Exceptions
{
    public class WorkflowException : Exception
    {
        public WorkflowException(string message)
                : base(message)
        {
        }
    }
}
