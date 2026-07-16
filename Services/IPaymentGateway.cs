using Billing.Api.Models;

namespace Billing.Api.Services;

public interface IPaymentGateway
{
    PaymentReceiptResponse ProcessPayment(CreateOrderRequest request);
}
