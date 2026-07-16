using Billing.Application.Models;

namespace Billing.Application.Exceptions;

public sealed class PaymentGatewayNotFoundException(PaymentGatewayType gatewayType)
    : Exception($"Payment gateway '{gatewayType}' is not available.")
{
    public PaymentGatewayType GatewayType { get; } = gatewayType;
}
