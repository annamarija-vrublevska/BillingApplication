namespace Billing.Api.Interfaces;

public interface IPaymentGatewayResolver
{
    IPaymentGateway Resolve(string gatewayName);
}