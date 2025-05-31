using FluentValidation;
using GenericAPI.DTOs;

namespace GenericAPI.Validators;

public class RegisterRequestDtoValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required")
            .Length(3, 50).WithMessage("Username must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores and hyphens")
            .Must(NotContainConsecutiveSpecialChars).WithMessage("Username cannot contain consecutive special characters");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .Length(5, 254).WithMessage("Email must be between 5 and 254 characters")
            .Must(BeAValidEmailDomain).WithMessage("Email domain is not allowed");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Length(8, 128).WithMessage("Password must be between 8 and 128 characters")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")
            .Must(NotContainCommonPasswords).WithMessage("Password is too common, please choose a stronger password")
            .Must(NotContainPersonalInfo).WithMessage("Password should not contain personal information");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("First name can only contain letters, spaces, hyphens and apostrophes");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .Length(2, 50).WithMessage("Last name must be between 2 and 50 characters")
            .Matches(@"^[a-zA-Z\s'-]+$").WithMessage("Last name can only contain letters, spaces, hyphens and apostrophes");

        // Cross-field validation
        RuleFor(x => x)
            .Must(x => !x.Password.ToLower().Contains(x.Username.ToLower()))
            .WithMessage("Password should not contain username")
            .When(x => !string.IsNullOrEmpty(x.Username) && !string.IsNullOrEmpty(x.Password));
    }

    private bool NotContainConsecutiveSpecialChars(string username)
    {
        return !username.Contains("__") && !username.Contains("--") && !username.Contains("_-") && !username.Contains("-_");
    }

    private bool BeAValidEmailDomain(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
            return false;

        var domain = email.Split('@')[1].ToLower();
        var blockedDomains = new[] { "tempmail.com", "10minutemail.com", "throwaway.email" };
        
        return !blockedDomains.Any(blocked => domain.Contains(blocked));
    }

    private bool NotContainCommonPasswords(string password)
    {
        var commonPasswords = new[]
        {
            "password", "123456789", "qwerty123", "password123", "admin123",
            "welcome123", "letmein123", "monkey123", "dragon123"
        };

        return !commonPasswords.Any(common => password.ToLower().Contains(common.ToLower()));
    }

    private bool NotContainPersonalInfo(string password)
    {
        // This is a simplified check - in a real application, you'd check against the actual user data
        var commonPersonalTerms = new[] { "birthday", "name", "email", "phone", "address" };
        return !commonPersonalTerms.Any(term => password.ToLower().Contains(term));
    }
}

public class LoginRequestDtoValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .Length(5, 254).WithMessage("Email must be between 5 and 254 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .Length(1, 128).WithMessage("Password must be between 1 and 128 characters");
    }
}

public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required")
            .Length(10, 500).WithMessage("Invalid refresh token format")
            .Must(BeValidTokenFormat).WithMessage("Invalid refresh token format");
    }

    private bool BeValidTokenFormat(string token)
    {
        // Basic token format validation - adjust based on your token format
        return !string.IsNullOrWhiteSpace(token) && 
               token.Length >= 10 && 
               !token.Contains(" ") &&
               System.Text.RegularExpressions.Regex.IsMatch(token, @"^[A-Za-z0-9+/=_-]+$");
    }
}
