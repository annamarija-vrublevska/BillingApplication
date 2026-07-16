using Billing.Application.Models;
using Billing.Domain.Models;
using PaymentResult = Billing.Application.Models.PaymentResult;

namespace Billing.Application.Interfaces;

public interface IPaymentGateway
{
    PaymentGatewayType GatewayType { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request,
        CancellationToken cancellationToken);
}
