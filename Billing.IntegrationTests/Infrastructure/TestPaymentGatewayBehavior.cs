namespace Billing.IntegrationTests.Infrastructure;

public enum TestPaymentGatewayBehavior
{
    Success = 1,
    Declined = 2,
    UnexpectedException = 3
}
