using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Domain.Models;

namespace Billing.Application.Services;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<PaymentGatewayType, IPaymentGateway> _gatewaysByType;

    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> paymentGateways)
    {
        var duplicateGatewayTypes = paymentGateways
            .GroupBy(gateway => gateway.GatewayType)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        if (duplicateGatewayTypes.Length > 0)
        {
            throw new InvalidOperationException(
                $"Multiple payment gateway implementations registered for: {string.Join(", ", duplicateGatewayTypes)}.");
        }

        _gatewaysByType = paymentGateways.ToDictionary(gateway => gateway.GatewayType, gateway => gateway);
    }

    public IPaymentGateway Resolve(PaymentGatewayType gatewayType)
    {
        if (_gatewaysByType.TryGetValue(gatewayType, out var gateway))
        {
            return gateway;
        }

        throw new PaymentGatewayNotFoundException(gatewayType);
    }
}