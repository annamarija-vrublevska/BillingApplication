using Billing.Api.Interfaces;
using Billing.Api.Models;

namespace Billing.Api.Services;

public sealed class SebPaymentGatewayMock : IPaymentGateway
{
    public string GatewayId => "SebPaymentGateway";
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(2);

    public async Task<PaymentReceiptResponse> ProcessPaymentAsync(CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await Task.Delay(ProcessingDelay, cancellationToken);

        return new PaymentReceiptResponse(
            OrderNumber: request.OrderNumber,
            Amount: request.Amount,
            Timestamp: DateTimeOffset.UtcNow,
            ConfirmationNumber: Guid.NewGuid().ToString("N"));
    }
}
