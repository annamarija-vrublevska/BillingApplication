namespace Billing.Application.Models;

public sealed record CreateOrderResult(
    string OrderNumber,
    decimal Amount,
    DateTimeOffset Timestamp,
    string ConfirmationNumber);