using Billing.Application.Interfaces;
using Billing.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(BillingDbContext dbContext) : IOrderRepository
{
    public async Task<Order?> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        return await dbContext.Orders
            .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);
        await dbContext.Orders.AddAsync(order, cancellationToken);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
