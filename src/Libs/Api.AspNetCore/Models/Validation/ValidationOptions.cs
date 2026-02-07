using System;
using System.Collections.Generic;
using System.Text;

namespace Api.AspNetCore.Models.Validation
{
    public class ValidationOptions
    {
        public string FieldName { get; set; }
        public bool IsRequired { get; set; }
        public int? MinLength { get; set; }
        public int? MaxLength { get; set; }
        public int? Length { get; set; }
        public bool IsAlpha { get; set; }
        public bool IsAlphaCyrillic { get; set; }
        public bool IsNumeric { get; set; }
        public bool IsEmail { get; set; }
        public char[] SpecialChars { get; set; }
        public Action<object, ValidationOptions> AdditionalValidation { get; set; }
    }
}
