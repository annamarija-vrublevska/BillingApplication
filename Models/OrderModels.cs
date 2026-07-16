namespace Billing.Api.Models;

public sealed record CreateOrderRequest(
    string OrderNumber,
    string UserId,
    decimal Amount,
    string PaymentGatewayId,
    string? Description);

public sealed record PaymentReceiptResponse(
    string OrderNumber,
    decimal Amount,
    DateTimeOffset Timestamp,
    string ConfirmationNumber);