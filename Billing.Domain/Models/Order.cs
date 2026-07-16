namespace Billing.Domain.Models;

public sealed class Order
{
    private Order()
    {
    }

    public Order(
        string orderNumber,
        string userId,
        decimal amount,
        string? description,
        PaymentGatewayType paymentGateway)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        if (!Enum.IsDefined(paymentGateway))
        {
            throw new ArgumentOutOfRangeException(nameof(paymentGateway));
        }

        Id = Guid.NewGuid();
        OrderNumber = orderNumber;
        UserId = userId;
        Amount = amount;
        Description = description;
        PaymentGateway = paymentGateway;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Description { get; init; }
    public PaymentGatewayType PaymentGateway { get; init; }
    public DateTime CreatedAt { get; init; }

    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public string? ConfirmationNumber { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? FailureReason { get; private set; }

    public bool IsEquivalentTo(
        string userId,
        decimal payableAmount,
        PaymentGatewayType paymentGateway,
        string? description)
    {
        return UserId == userId
            && Amount == payableAmount
            && PaymentGateway == paymentGateway
            && string.Equals(Description, description, StringComparison.Ordinal);
    }

    public void MarkAsFailed(string failureReason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(failureReason);

        Status = OrderStatus.Failed;
        FailureReason = failureReason;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsPaid(string confirmationNumber)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationNumber);

        Status = OrderStatus.Paid;
        ConfirmationNumber = confirmationNumber;
        ProcessedAt = DateTime.UtcNow;
        FailureReason = null;
    }

    public void MarkAsProcessing()
    {
        Status = OrderStatus.Processing;
    }
}
