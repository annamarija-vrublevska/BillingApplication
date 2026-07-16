using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.ExceptionHandlers;

public sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        var errors = validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => x.ErrorMessage)
                    .Distinct()
                    .ToArray());

        var problemDetails = new ValidationProblemDetails(errors)
        {
            Type = "/problems/validation-error",
            Title = "One or more validation errors occurred.",
            Status = StatusCodes.Status400BadRequest,
            Detail = "See the errors property for details.",
            Instance = httpContext.Request.Path
        };

        problemDetails.AddTraceId(httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}