using Billing.Application.Models;
using System.Text.Json.Serialization;

namespace Billing.Api.Models;

public sealed record CreateOrderRequest(
    string OrderNumber,
    string UserId,
    decimal Amount,
    [property: JsonPropertyName("paymentGateway")] PaymentGatewayType PaymentGatewayType,
    string? Description);