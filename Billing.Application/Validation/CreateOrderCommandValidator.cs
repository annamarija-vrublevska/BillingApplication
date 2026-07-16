using Billing.Application.Models;
using FluentValidation;

namespace Billing.Application.Validation;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .WithMessage("Order number is required.");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User id is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.PaymentGatewayType)
            .IsInEnum()
            .WithMessage("Payment gateway type is invalid.");
    }
}
