using Billing.Api.Models;

namespace Billing.Api.Services;

public sealed class PaymentGatewayMock : IPaymentGateway
{
    public PaymentReceiptResponse ProcessPayment(CreateOrderRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new PaymentReceiptResponse(
            OrderNumber: request.OrderNumber,
            Amount: request.Amount,
            Timestamp: DateTimeOffset.UtcNow,
            ConfirmationNumber: Guid.NewGuid().ToString("N"));
    }
}
