using Billing.Api.Models;

namespace Billing.Api.PaymentGateway;

public class PaymentGatewayMock
{
    public PaymentResult Process(decimal amount)
    {
        return PaymentResult.Success(Guid.NewGuid().ToString(), DateTime.Now);
    }
}