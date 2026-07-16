using Billing.Domain.Models;

namespace Billing.Application.Models;

public sealed record CreateOrderResult(
    string OrderNumber,
    decimal Amount,
    DateTimeOffset Timestamp,
    OrderStatus Status,
    string? ConfirmationNumber,
    string? FailureReason,
    bool IsIdempotentReplay);