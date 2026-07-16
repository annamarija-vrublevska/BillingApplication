using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Domain.Models;

namespace Billing.IntegrationTests.Infrastructure;

public sealed class ControlledPaymentGateway(
    PaymentGatewayType gatewayType,
    TestPaymentGatewayController controller)
    : IPaymentGateway
{
    public PaymentGatewayType GatewayType { get; } = gatewayType;

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        return controller.ProcessAsync(GatewayType, request, cancellationToken);
    }
}
