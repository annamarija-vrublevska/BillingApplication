using Billing.Application.Models;

namespace Billing.Application.Interfaces;

public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(PaymentGatewayType gatewayType);
}