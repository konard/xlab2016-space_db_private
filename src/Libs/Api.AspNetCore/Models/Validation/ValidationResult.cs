using System;
using System.Collections.Generic;
using System.Text;

namespace Api.AspNetCore.Models.Validation
{
    public class ValidationResult
    {
        public bool Success { get { return Errors?.Count == 0; } }

        public List<string> Errors = new List<string>();

        public ValidationResult()
        {
        }

        public ValidationResult(params ValidationResult[] results)
        {
            foreach (var result in results)
            {
                if (!result.Success)
                    Errors.AddRange(result.Errors);
            }
        }

        public ValidationResult(params string[] errors)
        {
            Errors.AddRange(errors);
        }
    }
}
