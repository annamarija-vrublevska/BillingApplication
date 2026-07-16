using Billing.Application.Interfaces;
using Billing.Infrastructure.Persistence;
using Billing.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("BillingDb")
            ?? "Data Source=billing.db";

        services.AddDbContext<BillingDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddScoped<IOrderRepository, OrderRepository>();

        return services;
    }
}
