namespace Billing.Application.Exceptions;

public sealed class OrderConflictException(string orderNumber)
    : Exception($"Order '{orderNumber}' already exists with different request data.")
{
    public string OrderNumber { get; } = orderNumber;
}
