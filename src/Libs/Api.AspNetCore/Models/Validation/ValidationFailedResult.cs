using System;
using System.Collections.Generic;
using System.Text;

namespace Api.AspNetCore.Models.Validation
{
    public class ValidationFailedResult : ValidationResult
    {
        public ValidationFailedResult(ValidationResult result)
            : base(result.Errors.ToArray())
        {
        }
    }
}
