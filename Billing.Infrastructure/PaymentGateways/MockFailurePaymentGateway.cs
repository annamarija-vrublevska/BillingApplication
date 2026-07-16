using Billing.Application.Interfaces;
using Billing.Application.Models;

namespace Billing.Infrastructure.PaymentGateways;

public sealed class MockFailurePaymentGateway : IPaymentGateway
{
    public PaymentGatewayType GatewayType => PaymentGatewayType.MockFailure;
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);
    private const string FailureMessage = "Mock failure gateway rejected the payment.";

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await Task.Delay(ProcessingDelay, cancellationToken);

        throw new InvalidOperationException(FailureMessage);
    }
}
