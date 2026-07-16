namespace Billing.Api.Models;

public sealed record CreateOrderRequest(
    string OrderNumber,
    string UserId,
    decimal Amount,
    string PaymentGatewayId,
    string? Description);