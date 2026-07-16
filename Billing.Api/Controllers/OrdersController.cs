using AutoMapper;
using Billing.Api.Models;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using Microsoft.AspNetCore.Mvc;

namespace Billing.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(IOrderAppService orderAppService, IMapper mapper) : Controller
{
    [HttpPost]
    [ProducesResponseType(typeof(PaymentReceiptResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaymentReceiptResponse>> Submit([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var command = mapper.Map<CreateOrderCommand>(request);
        var result = await orderAppService.ProcessOrder(command, cancellationToken);
        return Ok(mapper.Map<PaymentReceiptResponse>(result));
    }
}