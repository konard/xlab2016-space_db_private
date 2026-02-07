using System;
using System.Collections.Generic;
using System.Text;

namespace Data.Repository.Models.Rest
{
    public class OperationResult<TResult>
    {
        public bool Succeded { get; set; }
        public string ErrorMessage { get; set; }
        public bool ClientMistake { get; set; }
        public TResult Result { get; set; }

        public OperationResult(TResult result)
        {
            Result = result;
            Succeded = true;
        }

        public OperationResult(string errorMessage, bool clientMistake)
        {
            ErrorMessage = errorMessage;
            ClientMistake = clientMistake;
        }

        public OperationResult(string errorMessage)
            : this(errorMessage, false)
        {
        }
    }
}
