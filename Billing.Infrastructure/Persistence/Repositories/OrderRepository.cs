using Billing.Application.Interfaces;
using Billing.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Billing.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository(BillingDbContext dbContext) : IOrderRepository
{
    public async Task<Order> GetByOrderNumberAsync(
        string orderNumber,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        var order = await dbContext.Orders
            .SingleOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException($"Order with number '{orderNumber}' was not found.");
        }

        return order;
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(order);

        if (!await ExistsAsync(order.OrderNumber, cancellationToken))
        {
            await dbContext.Orders.AddAsync(order, cancellationToken);
        }
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> ExistsAsync(string orderNumber,
        CancellationToken cancellationToken)
    {
        return dbContext.Orders.AnyAsync(o => o.OrderNumber == orderNumber, cancellationToken);
    }
}
