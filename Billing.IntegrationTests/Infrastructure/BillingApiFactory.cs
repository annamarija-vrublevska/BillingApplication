using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Billing.IntegrationTests.Infrastructure;

public sealed class BillingApiFactory(Action<IServiceCollection>? configureServices = null)
    : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"billing-int-{Guid.NewGuid():N}.db");

    public TestPaymentGatewayController GatewayController { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:BillingDb"] = $"Data Source={_dbPath}"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<BillingDbContext>>();
            services.RemoveAll<BillingDbContext>();
            services.AddDbContext<BillingDbContext>(options => options.UseSqlite($"Data Source={_dbPath}"));

            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton(GatewayController);
            services.AddScoped<IPaymentGateway>(sp => new ControlledPaymentGateway(
                PaymentGatewayType.MockSuccess,
                sp.GetRequiredService<TestPaymentGatewayController>()));
            services.AddScoped<IPaymentGateway>(sp => new ControlledPaymentGateway(
                PaymentGatewayType.MockFailure,
                sp.GetRequiredService<TestPaymentGatewayController>()));

            configureServices?.Invoke(services);
        });
    }

    public async Task<int> CountOrdersAsync(string orderNumber)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        return await dbContext.Orders.AsNoTracking().CountAsync(order => order.OrderNumber == orderNumber);
    }

    public async Task<decimal?> GetOrderAmountAsync(string orderNumber)
    {
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        return await dbContext.Orders
            .AsNoTracking()
            .Where(order => order.OrderNumber == orderNumber)
            .Select(order => (decimal?)order.Amount)
            .SingleOrDefaultAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch
        {
            // Best-effort cleanup for temp DB file.
        }
    }
}
