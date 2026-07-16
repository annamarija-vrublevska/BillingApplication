using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.ExceptionHandlers;

internal static class ProblemDetailsTraceIdExtensions
{
    public static void AddTraceId(this ProblemDetails problemDetails, string traceIdentifier)
    {
        if (!problemDetails.Extensions.ContainsKey("traceId"))
        {
            problemDetails.Extensions["traceId"] = traceIdentifier;
        }
    }
}
