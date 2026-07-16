using AutoMapper;
using Billing.Api.Models;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderAppService orderAppService, IMapper mapper) : ControllerBase
{
    /// <summary>
    /// Submits an order for payment processing.
    /// </summary>
    /// <remarks>
    /// OrderNumber is the idempotency key. Repeating the same request returns the existing receipt without charging twice.
    /// Sending the same OrderNumber with different request data returns a conflict.
    /// </remarks>
    [HttpPost]
    [ProducesResponseType(typeof(PaymentReceiptResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(PaymentReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PaymentReceiptResponse>> SubmitOrderAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateOrderCommand>(request);
        var result = await orderAppService.ProcessOrderAsync(command, cancellationToken);
        var response = mapper.Map<PaymentReceiptResponse>(result);

        if (result.IsExistingOrder)
        {
            return Ok(response);
        }

        return CreatedAtRoute(
            "GetOrderByNumber",
            new { orderNumber = response.OrderNumber },
            response);
    }

    /// <summary>
    /// Gets the current order state by order number.
    /// </summary>
    [HttpGet("{orderNumber}", Name = "GetOrderByNumber")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<OrderResponse>> GetOrderAsync(
        [FromRoute] string orderNumber,
        CancellationToken cancellationToken)
    {
        var result = await orderAppService.GetOrderAsync(orderNumber, cancellationToken);
        return Ok(mapper.Map<OrderResponse>(result));
    }
}