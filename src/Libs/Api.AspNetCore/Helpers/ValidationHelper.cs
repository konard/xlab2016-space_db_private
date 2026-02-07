using Api.AspNetCore.Models.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Api.AspNetCore.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsEmail(this string s)
        {
            return new EmailAddressAttribute().IsValid(s);
        }
        public static bool CheckInRange(this string s, int? min, int? max)
        {
            if (s == null) 
                return false;
            if (min.HasValue && s.Length < min)
                return false;
            if (max.HasValue && s.Length > max)
                return false;
            return true;
        }
        public static bool CheckChars(this string s, bool isAlpha, bool isCyrillicCharacters,
            bool isNumeric,
            params char[] specials)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            var checks = new List<Func<char, bool>>();
            if (isAlpha)
                checks.Add((_) => ('a' <= _ && _ <= 'z') || ('A' <= _ && _ <= 'Z'));
            if (isCyrillicCharacters)
                checks.Add((_) => ('а' <= _ && _ <= 'я') || ('А' <= _ && _ <= 'Я'));
            if (isNumeric)
                checks.Add((_) => '0' <= _ && _ <= '9');
            if (specials?.Length > 0)
                checks.Add((_) => specials.Contains(_));
            Func<char, bool> checkAll = (_) => checks.Any(checkItem => checkItem(_));
            if (checks.Count > 0 && !s.All(_ => checkAll(_)))
                return false;
            return true;
        }
        public static string FieldCharsNotValid(this object source, string field) =>
            $"{field} contains invalid character";
        public static string FieldShouldBeEmail(this object source, string field) =>
            $"{field} should be email";
        public static string FieldNotInRange(this object source, string field, int? min, int? max)
        {
            if (min.HasValue && max.HasValue)
                return $"{field} not in {min}-{max} range";
            else if (min.HasValue)
                return $"{field} length is less than {min}";
            else if (max.HasValue)
                return $"{field} length is greater than {max}";
            return string.Empty;
        }
        public static string FieldIsEmpty(this object source, string field) => $"{field} is empty";
        public static string FieldsAreEmpty(this object source, List<string> fields) => $"{string.Join(" or ", fields)} are empty";
        public static string FieldNotTooComplex(this object source, string field) => $"{field} complexity failed";

        public static void ValidateAsPhoneNumber(this string phoneNumber, string fieldName,
            Models.Validation.ValidationResult result)
        {
            phoneNumber.Validate(new ValidationOptions
            {
                FieldName = fieldName,
                IsRequired = true,
                IsNumeric = true,
                SpecialChars = new char[] { '+' },
                MinLength = 11,
                MaxLength = 15,
                AdditionalValidation = (_, options) =>
                {
                    if ((_ as string).IndexOf('+') >= 1)
                        result.Errors.Add($"{options.FieldName} + sign should be in start");
                }
            }, result);
        }

        public static void ValidateAsPassword(this string password, string fieldName,
            Models.Validation.ValidationResult result)
        {
            password.Validate(new ValidationOptions
            {
                FieldName = fieldName,
                IsRequired = true,
                IsAlpha = true,
                IsNumeric = true,
                MinLength = 8,
                MaxLength = 40,
                SpecialChars = new char[] { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '-',
                    '_', '+', '=', ';', ':', ',', '.', '/', '?', '\\', '|', '`', '~', '[', ']',
                    '{', '}', '^', '\'', '"', '<', '>', '~', '.', },
                AdditionalValidation = (_, options) =>
                {
                    if ((_ as string).All(i => char.IsDigit(i)))
                        result.Errors.Add($"{options.FieldName} shouldn't be only digits");
                    // TODO: complexity
                }
            }, result);
        }

        public static string ValidateSpaces(this string value)
        {
            if (value == null || value == "")
                return value;

            if (value[0] == ' ' && value.ToString()[value.Length - 1] == ' ')
            {
                var returnValue = "";
                for (int i = 1; i < value.Length - 1; i++)
                {
                    returnValue += value[i];
                }
                return returnValue;
            }
            return value;
        }

        public static void Validate<T>(this T value, ValidationOptions options,
            Models.Validation.ValidationResult result)
        {
            if (value is string s)
            {
                if (options.IsRequired && string.IsNullOrEmpty(s))
                    result.Errors.Add(result.FieldIsEmpty(options.FieldName));
                if (!s.CheckInRange(options.MinLength, options.MaxLength))
                    result.Errors.Add(result.FieldNotInRange(options.FieldName,
                        options.MinLength, options.MaxLength));
                if (!s.CheckChars(options.IsAlpha, options.IsAlphaCyrillic, options.IsNumeric,
                    options.SpecialChars))
                    result.Errors.Add(result.FieldCharsNotValid(options.FieldName));
                if (options.IsEmail && !s.IsEmail())
                    result.Errors.Add(result.FieldShouldBeEmail(options.FieldName));

            }
            else
            {
                if (options.IsRequired && value == null)
                    result.Errors.Add(result.FieldIsEmpty(options.FieldName));
            }

            if (value != null)
                options.AdditionalValidation?.Invoke(value, options);
        }
    }
}
