using Billing.Application.Models;
using FluentValidation;

namespace Billing.Application.Validation;

public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.UserId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Amount)
            .GreaterThan(0);

        RuleFor(x => x.PaymentGatewayType)
            .IsInEnum();

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}
