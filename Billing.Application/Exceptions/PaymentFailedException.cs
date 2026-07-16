namespace Billing.Application.Exceptions;

public sealed class PaymentFailedException(string orderNumber, Exception innerException)
    : Exception($"Payment processing failed for order '{orderNumber}'.", innerException)
{
    public string OrderNumber { get; } = orderNumber;
}
