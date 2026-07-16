using Billing.Api.Models;

namespace Billing.Api.Interfaces;

public interface IPaymentGateway
{
    string GatewayId { get; }
    Task<PaymentReceiptResponse> ProcessPaymentAsync(CreateOrderRequest request,
        CancellationToken cancellationToken);
}
