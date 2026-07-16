using Billing.Application.Interfaces;
using Billing.Application.Models;

namespace Billing.Infrastructure.PaymentGateways;

public sealed class SwedbankPaymentGatewayMock : IPaymentGateway
{
    public string GatewayId => "SwedbankPaymentGateway";
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
