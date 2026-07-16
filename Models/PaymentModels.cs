namespace Billing.Api.Models;

public sealed record PaymentResult
{
    public bool IsSuccessful { get; init; }

    public string? ConfirmationNumber { get; init; }

    public string? ErrorCode { get; init; }

    public string? ErrorMessage { get; init; }

    public bool IsTransientFailure { get; init; }

    public DateTime ProcessedAt { get; init; }

    public static PaymentResult Success(
        string confirmationNumber,
        DateTime processedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationNumber);

        return new PaymentResult
        {
            IsSuccessful = true,
            ConfirmationNumber = confirmationNumber,
            ProcessedAt = processedAt
        };
    }

    public static PaymentResult Failure(
        string errorCode,
        string errorMessage,
        bool isTransientFailure,
        DateTime processedAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        return new PaymentResult
        {
            IsSuccessful = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            IsTransientFailure = isTransientFailure,
            ProcessedAt = processedAt
        };
    }
}