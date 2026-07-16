namespace Billing.Application.Interfaces;

public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(string gatewayName);
}