using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Data.Repository.Models.Rest.RemoveOperationResult;

namespace Data.Repository.Models.Rest

{
    public class RemoveOperationResult : OperationResult<RemoveResult>
    {
        public RemoveOperationResult(RemoveResult result) : 
            base(result)
        {
            Result = result;
        }

        public RemoveOperationResult(string errorMessage, bool clientMistake) : 
            base(errorMessage, clientMistake)
        {
        }

        public RemoveOperationResult(string errorMessage) : 
            base(errorMessage)
        {
        }

        public enum RemoveResult : int
        {
            Undefined = 0,
            Success = 1,
            NotFound = 2,
            ReferentialIntegrityViolation = 3
        }
    }
}
