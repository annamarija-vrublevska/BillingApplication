using Billing.Application.Models;

namespace Billing.Application.Interfaces;

public interface IOrderAppService
{
    Task<CreateOrderResult> ProcessOrder(CreateOrderCommand command, CancellationToken cancellationToken);
}