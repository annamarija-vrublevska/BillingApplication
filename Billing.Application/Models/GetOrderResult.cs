using Billing.Domain.Models;

namespace Billing.Application.Models;

public sealed record GetOrderResult(
    string OrderNumber,
    string UserId,
    decimal Amount,
    PaymentGatewayType PaymentGateway,
    OrderStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    string? ConfirmationNumber,
    string? FailureReason);
