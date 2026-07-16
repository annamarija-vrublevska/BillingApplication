namespace Billing.Application.Exceptions;

public sealed class OrderNotFoundException(string orderNumber)
    : Exception($"Order '{orderNumber}' was not found.")
{
    public string OrderNumber { get; } = orderNumber;
}
