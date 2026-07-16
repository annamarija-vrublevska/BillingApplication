using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Domain.Models;

namespace Billing.Infrastructure.PaymentGateways;

public sealed class MockRetryPaymentGateway : IPaymentGateway
{
    public PaymentGatewayType GatewayType => PaymentGatewayType.MockRetry;
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(1);
    private const int TimeoutsBeforeSuccess = 2;
    private int _attempt;

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _attempt++;
        await Task.Delay(ProcessingDelay, cancellationToken);

        if (_attempt <= TimeoutsBeforeSuccess)
        {
            throw new TimeoutException("Mock retry gateway timed out.");
        }

        return new PaymentResult(
            OrderNumber: request.OrderNumber,
            Amount: request.Amount,
            Timestamp: DateTimeOffset.UtcNow,
            ConfirmationNumber: Guid.NewGuid().ToString("N"));
    }
}
