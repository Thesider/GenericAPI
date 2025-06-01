using FluentValidation;
using GenericAPI.DTOs;

namespace GenericAPI.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("Valid User ID is required")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.ShippingAddress)
            .Length(10, 500).WithMessage("Shipping address must be between 10 and 500 characters")
            .When(x => !string.IsNullOrEmpty(x.ShippingAddress));

        RuleFor(x => x.OrderItems)
            .NotEmpty().WithMessage("At least one order item is required")
            .Must(HaveValidOrderItems).WithMessage("All order items must be valid");

        RuleForEach(x => x.OrderItems)
            .SetValidator(new CreateOrderItemDtoValidator());
    }

    private bool HaveValidOrderItems(List<CreateOrderItemDto> orderItems)
    {
        return orderItems.All(item => item.ProductId > 0 && item.Quantity > 0);
    }
}

public class CreateOrderItemDtoValidator : AbstractValidator<CreateOrderItemDto>
{
    public CreateOrderItemDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Valid Product ID is required");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Quantity must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Quantity cannot exceed 1000 items per product");
    }
}

public class UpdateOrderStatusDtoValidator : AbstractValidator<UpdateOrderStatusDto>
{
    private readonly string[] _validStatuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

    public UpdateOrderStatusDtoValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeValidStatus).WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}");
    }

    private bool BeValidStatus(string? status)
    {
        return !string.IsNullOrEmpty(status) && _validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}

public class OrderSearchCriteriaDtoValidator : AbstractValidator<OrderSearchCriteriaDto>
{
    private readonly string[] _validStatuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

    public OrderSearchCriteriaDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(BeValidStatus).WithMessage($"Status must be one of: {string.Join(", ", _validStatuses)}")
            .When(x => !string.IsNullOrEmpty(x.Status));

        RuleFor(x => x.StartDate)
            .LessThanOrEqualTo(x => x.EndDate).WithMessage("Start date must be before or equal to end date")
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

        RuleFor(x => x.EndDate)
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1)).WithMessage("End date cannot be in the future")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("User ID must be greater than 0")
            .When(x => x.UserId.HasValue);

        RuleFor(x => x.MinAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum amount cannot be negative")
            .LessThan(x => x.MaxAmount).WithMessage("Minimum amount must be less than maximum amount")
            .When(x => x.MinAmount.HasValue && x.MaxAmount.HasValue);

        RuleFor(x => x.MaxAmount)
            .GreaterThan(0).WithMessage("Maximum amount must be greater than 0")
            .LessThan(1000000).WithMessage("Maximum amount must be less than 1,000,000")
            .When(x => x.MaxAmount.HasValue);
    }

    private bool BeValidStatus(string? status)
    {
        return !string.IsNullOrEmpty(status) && _validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
}
