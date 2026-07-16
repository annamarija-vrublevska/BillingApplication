using Billing.Domain.Models;

namespace Billing.Api.Models;

public sealed record PaymentReceiptResponse(
    string OrderNumber,
    decimal Amount,
    DateTimeOffset Timestamp,
    OrderStatus Status,
    string? ConfirmationNumber,
    string? FailureReason);