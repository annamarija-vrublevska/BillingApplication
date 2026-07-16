namespace Billing.Application.Models;

public sealed record PaymentRequest(
    string OrderNumber,
    string UserId,
    decimal Amount,
    PaymentGatewayType PaymentGatewayType,
    string Description);