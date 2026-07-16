using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Domain.Models;

namespace Billing.Infrastructure.PaymentGateways;

public sealed class MockSuccessPaymentGateway : IPaymentGateway
{
    public PaymentGatewayType GatewayType => PaymentGatewayType.MockSuccess;
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await Task.Delay(ProcessingDelay, cancellationToken);

        return new PaymentResult(
            OrderNumber: request.OrderNumber,
            Amount: request.Amount,
            Timestamp: DateTimeOffset.UtcNow,
            ConfirmationNumber: Guid.NewGuid().ToString("N"));
    }
}
