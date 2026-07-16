namespace Billing.Api.Models;

public sealed record PaymentReceiptResponse(
    string OrderNumber,
    decimal Amount,
    DateTimeOffset Timestamp,
    string ConfirmationNumber);