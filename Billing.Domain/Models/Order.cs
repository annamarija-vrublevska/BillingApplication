namespace Billing.Domain.Models;

public sealed class Order
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public int PaymentGatewayId { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public string? ConfirmationNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}
