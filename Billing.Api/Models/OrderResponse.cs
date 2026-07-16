using Billing.Domain.Models;
using System.Text.Json.Serialization;

namespace Billing.Api.Models;

public sealed record OrderResponse(
    string OrderNumber,
    string UserId,
    decimal Amount,
    [property: JsonPropertyName("paymentGateway")] PaymentGatewayType PaymentGateway,
    OrderStatus Status,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ProcessedAt,
    string? ConfirmationNumber,
    string? FailureReason);
