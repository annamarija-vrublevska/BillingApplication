using Billing.Api.Models;
using Billing.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : Controller
{
    private readonly IPaymentGateway _paymentGateway;

    public OrdersController(IPaymentGateway paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    [HttpPost]
    [ProducesResponseType(typeof(PaymentReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PaymentReceiptResponse> Submit([FromBody] CreateOrderRequest request)
    {
        if (request is null)
        {
            return BadRequest("Request body is required.");
        }

        if (string.IsNullOrWhiteSpace(request.OrderNumber))
        {
            ModelState.AddModelError(nameof(request.OrderNumber), "Order number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            ModelState.AddModelError(nameof(request.UserId), "User id is required.");
        }

        if (request.Amount <= 0)
        {
            ModelState.AddModelError(nameof(request.Amount), "Amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(request.PaymentGatewayId))
        {
            ModelState.AddModelError(nameof(request.PaymentGatewayId), "Payment gateway id is required.");
        }

        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var receipt = _paymentGateway.ProcessPayment(request);

        return Ok(receipt);
    }
}