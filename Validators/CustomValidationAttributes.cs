using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace GenericAPI.Validators
{
    /// <summary>
    /// Custom validation attribute for strong passwords
    /// </summary>
    public class StrongPasswordAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string password)
                return false;

            // At least 8 characters, one uppercase, one lowercase, one digit, one special character
            var strongPasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$";
            return Regex.IsMatch(password, strongPasswordPattern);
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
        }
    }

    /// <summary>
    /// Custom validation attribute for positive numbers
    /// </summary>
    public class PositiveNumberAttribute : ValidationAttribute
    {
        private readonly bool _allowZero;

        public PositiveNumberAttribute(bool allowZero = false)
        {
            _allowZero = allowZero;
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return true; // Let Required attribute handle null validation

            if (value is decimal decimalValue)
                return _allowZero ? decimalValue >= 0 : decimalValue > 0;
            
            if (value is double doubleValue)
                return _allowZero ? doubleValue >= 0 : doubleValue > 0;
            
            if (value is float floatValue)
                return _allowZero ? floatValue >= 0 : floatValue > 0;
            
            if (value is int intValue)
                return _allowZero ? intValue >= 0 : intValue > 0;
            
            if (value is long longValue)
                return _allowZero ? longValue >= 0 : longValue > 0;

            return false;
        }

        public override string FormatErrorMessage(string name)
        {
            return _allowZero 
                ? $"{name} must be zero or positive."
                : $"{name} must be a positive number.";
        }
    }

    /// <summary>
    /// Custom validation attribute for future dates
    /// </summary>
    public class FutureDateAttribute : ValidationAttribute
    {
        private readonly bool _allowToday;

        public FutureDateAttribute(bool allowToday = false)
        {
            _allowToday = allowToday;
        }

        public override bool IsValid(object? value)
        {
            if (value is not DateTime dateValue)
                return true; // Let Required attribute handle null validation

            var today = DateTime.Today;
            return _allowToday ? dateValue.Date >= today : dateValue.Date > today;
        }

        public override string FormatErrorMessage(string name)
        {
            return _allowToday 
                ? $"{name} must be today or a future date."
                : $"{name} must be a future date.";
        }
    }

    /// <summary>
    /// Custom validation attribute for phone numbers
    /// </summary>
    public class PhoneNumberAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value is not string phoneNumber)
                return true; // Let Required attribute handle null validation

            // Remove all non-digit characters
            var digitsOnly = Regex.Replace(phoneNumber, @"[^\d]", "");
            
            // Check if it's a valid length (10-15 digits)
            return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must be a valid phone number with 10-15 digits.";
        }
    }

    /// <summary>
    /// Custom validation attribute for file extensions
    /// </summary>
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;

        public AllowedExtensionsAttribute(params string[] extensions)
        {
            _extensions = extensions;
        }

        public override bool IsValid(object? value)
        {
            if (value is not string fileName)
                return true; // Let Required attribute handle null validation

            var extension = System.IO.Path.GetExtension(fileName);
            return _extensions.Contains(extension.ToLowerInvariant());
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must have one of the following extensions: {string.Join(", ", _extensions)}.";
        }
    }

    /// <summary>
    /// Custom validation attribute for maximum file size
    /// </summary>
    public class MaxFileSizeAttribute : ValidationAttribute
    {
        private readonly int _maxFileSize;

        public MaxFileSizeAttribute(int maxFileSize)
        {
            _maxFileSize = maxFileSize;
        }

        public override bool IsValid(object? value)
        {
            if (value is not Microsoft.AspNetCore.Http.IFormFile file)
                return true; // Let Required attribute handle null validation

            return file.Length <= _maxFileSize;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"{name} must not exceed {_maxFileSize / 1024 / 1024} MB.";
        }
    }
}
