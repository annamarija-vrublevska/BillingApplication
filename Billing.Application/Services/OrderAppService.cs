using AutoMapper;
using Billing.Application.Interfaces;
using Billing.Application.Models;

namespace Billing.Application.Services;

public class OrderAppService(IPaymentGatewayResolver paymentGatewayResolver, IMapper mapper) : IOrderAppService
{
    public async Task<CreateOrderResult> ProcessOrder(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var gateway = paymentGatewayResolver.Resolve(command.PaymentGatewayId);
        var paymentRequest = mapper.Map<PaymentRequest>(command);
        var paymentResult = await gateway.ProcessPaymentAsync(paymentRequest, cancellationToken);
        return mapper.Map<CreateOrderResult>(paymentResult);
    }
}