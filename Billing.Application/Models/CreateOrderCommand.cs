namespace Billing.Application.Models;

public sealed record CreateOrderCommand(
    string OrderNumber,
    string UserId,
    decimal Amount,
    PaymentGatewayType PaymentGatewayType,
    string? Description);