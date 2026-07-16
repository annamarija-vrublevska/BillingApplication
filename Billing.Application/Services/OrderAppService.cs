using AutoMapper;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using FluentValidation;

namespace Billing.Application.Services;

public class OrderAppService(
    IPaymentGatewayResolver paymentGatewayResolver,
    IMapper mapper,
    IValidator<CreateOrderCommand> createOrderCommandValidator) : IOrderAppService
{
    public async Task<CreateOrderResult> ProcessOrder(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        await createOrderCommandValidator.ValidateAndThrowAsync(command, cancellationToken);

        var gateway = paymentGatewayResolver.Resolve(command.PaymentGatewayId);
        var paymentRequest = mapper.Map<PaymentRequest>(command);
        var paymentResult = await gateway.ProcessPaymentAsync(paymentRequest, cancellationToken);
        return mapper.Map<CreateOrderResult>(paymentResult);
    }
}