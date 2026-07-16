using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Domain.Models;

namespace Billing.Application.Services;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly Dictionary<PaymentGatewayType, IPaymentGateway> _gatewaysByType;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> paymentGateways)
    {
        _gatewaysByType = paymentGateways.ToDictionary(
            gateway => gateway.GatewayType);
    }

    public IPaymentGateway Resolve(PaymentGatewayType gatewayType)
    {
        return _gatewaysByType.TryGetValue(gatewayType, out var gateway)
            ? gateway
            : throw new PaymentGatewayNotFoundException(gatewayType);
    }
}