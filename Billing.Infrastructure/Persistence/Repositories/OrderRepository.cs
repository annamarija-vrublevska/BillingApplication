using Billing.Application.Interfaces;
using Billing.Application.Exceptions;
using Billing.Domain.Models;
using Microsoft.Data.Sqlite;
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
        return SaveChangesInternalAsync(cancellationToken);
    }

    private async Task SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueOrderNumberViolation(exception))
        {
            var conflictingOrderNumber = exception.Entries
                .Select(entry => entry.Entity)
                .OfType<Order>()
                .Select(order => order.OrderNumber)
                .FirstOrDefault() ?? string.Empty;

            throw new OrderConflictException(conflictingOrderNumber);
        }
    }

    private static bool IsUniqueOrderNumberViolation(DbUpdateException exception)
    {
        if (exception.InnerException is not SqliteException sqliteException)
        {
            return false;
        }

        return sqliteException is { SqliteErrorCode: 19, SqliteExtendedErrorCode: 2067 };
    }
}
