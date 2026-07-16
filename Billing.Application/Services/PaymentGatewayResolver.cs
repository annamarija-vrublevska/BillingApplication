using Billing.Application.Interfaces;

namespace Billing.Application.Services;

public sealed class PaymentGatewayResolver : IPaymentGatewayResolver
{
    private readonly IReadOnlyDictionary<string, IPaymentGateway> _paymentGateways;
    public PaymentGatewayResolver(IEnumerable<IPaymentGateway> paymentGateways)
    {
        _paymentGateways = paymentGateways.ToDictionary(g => g.GatewayId, g => g);
    }
    public IPaymentGateway Resolve(string gatewayName)
    {
        if (_paymentGateways.TryGetValue(gatewayName, out var gateway))
        {
            return gateway;
        }

        throw new ArgumentException($"Payment gateway '{gatewayName}' not found.", nameof(gatewayName));
    }
}