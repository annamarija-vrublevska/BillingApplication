namespace Billing.Application.Models;

public sealed record PaymentResult(
    string OrderNumber,
    decimal Amount,
    DateTimeOffset Timestamp,
    string ConfirmationNumber);