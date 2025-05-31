using FluentValidation;
using GenericAPI.DTOs;

namespace GenericAPI.Validators;

public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required")
            .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Product name contains invalid characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Product description is required")
            .Length(10, 1000).WithMessage("Product description must be between 10 and 1000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThan(1000000).WithMessage("Price must be less than 1,000,000")
            .ScalePrecision(2, 18).WithMessage("Price can have maximum 2 decimal places");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative")
            .LessThan(100000).WithMessage("Stock quantity must be less than 100,000");

        RuleFor(x => x.ImageUrl)
            .Must(BeAValidUrl).WithMessage("Image URL is not valid")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .Length(2, 100).WithMessage("Product name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_\.]+$").WithMessage("Product name contains invalid characters")
            .When(x => !string.IsNullOrEmpty(x.Name));

        RuleFor(x => x.Description)
            .Length(10, 1000).WithMessage("Product description must be between 10 and 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0")
            .LessThan(1000000).WithMessage("Price must be less than 1,000,000")
            .ScalePrecision(2, 18).WithMessage("Price can have maximum 2 decimal places")
            .When(x => x.Price.HasValue);

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative")
            .LessThan(100000).WithMessage("Stock quantity must be less than 100,000")
            .When(x => x.StockQuantity.HasValue);

        RuleFor(x => x.ImageUrl)
            .Must(BeAValidUrl).WithMessage("Image URL is not valid")
            .When(x => !string.IsNullOrEmpty(x.ImageUrl));
    }

    private bool BeAValidUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return true;

        return Uri.TryCreate(url, UriKind.Absolute, out var result)
               && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public class UpdateProductStockDtoValidator : AbstractValidator<UpdateProductStockDto>
{
    public UpdateProductStockDtoValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative")
            .LessThan(100000).WithMessage("Quantity must be less than 100,000");
    }
}
