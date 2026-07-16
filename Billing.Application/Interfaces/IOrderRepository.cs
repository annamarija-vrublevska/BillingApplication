using Billing.Domain.Models;

namespace Billing.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(
        CancellationToken cancellationToken);
}