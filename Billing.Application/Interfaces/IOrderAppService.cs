using Billing.Application.Models;

namespace Billing.Application.Interfaces;

public interface IOrderAppService
{
    Task<CreateOrderResult> ProcessOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken);
    Task<GetOrderResult> GetOrderAsync(string orderNumber, CancellationToken cancellationToken);
}