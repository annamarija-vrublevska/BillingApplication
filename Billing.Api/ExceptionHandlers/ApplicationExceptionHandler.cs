using Billing.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.ExceptionHandlers;

public sealed class ApplicationExceptionHandler(
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, type, title) = exception switch
        {
            PaymentGatewayNotFoundException => (
                StatusCodes.Status400BadRequest,
                "/problems/payment-gateway-not-found",
                "Payment gateway not found"),
            OrderConflictException => (
                StatusCodes.Status409Conflict,
                "/problems/order-conflict",
                "Order conflict"),
            PaymentFailedException => (
                StatusCodes.Status422UnprocessableEntity,
                "/problems/payment-failed",
                "Payment failed"),
            OrderNotFoundException => (
                StatusCodes.Status404NotFound,
                "/problems/order-not-found",
                "Resource not found"),
            _ => (0, string.Empty, string.Empty)
        };

        if (status == 0)
        {
            return false;
        }

        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = status,
            Detail = exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.AddTraceId(httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
