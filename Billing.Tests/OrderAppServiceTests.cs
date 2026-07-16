using AutoMapper;
using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Application.Mapping;
using Billing.Application.Models;
using Billing.Application.Services;
using Billing.Application.Validation;
using Billing.Domain.Models;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Billing.Tests;

public sealed class OrderAppServiceTests
{
    [Fact]
    public async Task ProcessOrder_WhenNewOrderIsProcessed_ReturnsPaidResultWithReplayFalse()
    {
        var repository = new FakeOrderRepository();
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockSuccess);
        var service = CreateService(repository, gateway);
        var command = CreateValidCommand();

        var result = await service.ProcessOrder(command, CancellationToken.None);

        result.OrderNumber.Should().Be(command.OrderNumber);
        result.Amount.Should().Be(command.Amount);
        result.Status.Should().Be(OrderStatus.Paid);
        result.ConfirmationNumber.Should().Be("CONF-MOCK");
        result.FailureReason.Should().BeNull();
        result.IsIdempotentReplay.Should().BeFalse();
        gateway.CallCount.Should().Be(1);
        repository.AddCalls.Should().Be(1);
    }

    [Fact]
    public async Task ProcessOrder_WhenOrderNumberIsEmpty_ThrowsValidationException()
    {
        var repository = new FakeOrderRepository();
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockSuccess);
        var service = CreateService(repository, gateway);
        var command = CreateValidCommand() with { OrderNumber = string.Empty };

        Func<Task> act = () => service.ProcessOrder(command, CancellationToken.None);

        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
        exceptionAssertion.Which.Errors.Select(error => error.PropertyName)
            .Should().Contain(nameof(CreateOrderCommand.OrderNumber));
        gateway.CallCount.Should().Be(0);
        repository.AddCalls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOrder_WhenAmountIsNotPositive_ThrowsValidationException()
    {
        var repository = new FakeOrderRepository();
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockSuccess);
        var service = CreateService(repository, gateway);
        var command = CreateValidCommand() with { Amount = 0 };

        Func<Task> act = () => service.ProcessOrder(command, CancellationToken.None);

        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
        exceptionAssertion.Which.Errors.Select(error => error.PropertyName)
            .Should().Contain(nameof(CreateOrderCommand.Amount));
        gateway.CallCount.Should().Be(0);
        repository.AddCalls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOrder_WhenPaymentGatewayTypeIsInvalid_ThrowsValidationException()
    {
        var repository = new FakeOrderRepository();
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockSuccess);
        var service = CreateService(repository, gateway);
        var command = CreateValidCommand() with { PaymentGatewayType = (PaymentGatewayType)999 };

        Func<Task> act = () => service.ProcessOrder(command, CancellationToken.None);

        var exceptionAssertion = await act.Should().ThrowAsync<ValidationException>();
        exceptionAssertion.Which.Errors.Select(error => error.PropertyName)
            .Should().Contain(nameof(CreateOrderCommand.PaymentGatewayType));
        gateway.CallCount.Should().Be(0);
        repository.AddCalls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOrder_WhenEquivalentOrderExists_ReturnsStoredReceiptWithoutGatewayCall()
    {
        var existingOrder = new Order(
            orderNumber: "ORD-100",
            userId: "user-1",
            amount: 125.50m,
            description: "Test",
            paymentGatewayId: (int)PaymentGatewayType.MockSuccess);
        existingOrder.MarkAsPaid("CONF-100");

        var repository = new FakeOrderRepository(existingOrder);
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockSuccess);
        var service = CreateService(repository, gateway);

        var command = new CreateOrderCommand(
            OrderNumber: "ORD-100",
            UserId: "user-1",
            Amount: 125.50m,
            PaymentGatewayType: PaymentGatewayType.MockSuccess,
            Description: "Test");
        var result = await service.ProcessOrder(command, CancellationToken.None);

        result.OrderNumber.Should().Be("ORD-100");
        result.Amount.Should().Be(125.50m);
        result.ConfirmationNumber.Should().Be("CONF-100");
        result.Status.Should().Be(OrderStatus.Paid);
        result.FailureReason.Should().BeNull();
        result.IsIdempotentReplay.Should().BeTrue();
        gateway.CallCount.Should().Be(0);
        repository.AddCalls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOrder_WhenEquivalentFailedOrderExists_ReturnsStoredFailedResultWithoutGatewayCall()
    {
        var existingOrder = new Order(
            orderNumber: "ORD-FAILED-1",
            userId: "user-1",
            amount: 44.10m,
            description: "Retry me",
            paymentGatewayId: (int)PaymentGatewayType.MockFailure);
        existingOrder.MarkAsFailed("Gateway timeout");

        var repository = new FakeOrderRepository(existingOrder);
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockFailure);
        var service = CreateService(repository, gateway);

        var command = new CreateOrderCommand(
            OrderNumber: "ORD-FAILED-1",
            UserId: "user-1",
            Amount: 44.10m,
            PaymentGatewayType: PaymentGatewayType.MockFailure,
            Description: "Retry me");

        var result = await service.ProcessOrder(command, CancellationToken.None);

        result.OrderNumber.Should().Be("ORD-FAILED-1");
        result.Amount.Should().Be(44.10m);
        result.Status.Should().Be(OrderStatus.Failed);
        result.ConfirmationNumber.Should().BeNull();
        result.FailureReason.Should().Be("Gateway timeout");
        result.IsIdempotentReplay.Should().BeTrue();
        gateway.CallCount.Should().Be(0);
        repository.AddCalls.Should().Be(0);
    }

    [Fact]
    public async Task ProcessOrder_WhenOrderNumberExistsWithDifferentPayload_ThrowsOrderConflictException()
    {
        var existingOrder = new Order(
            orderNumber: "ORD-200",
            userId: "user-1",
            amount: 200m,
            description: "Original",
            paymentGatewayId: (int)PaymentGatewayType.MockSuccess);
        existingOrder.MarkAsPaid("CONF-200");

        var repository = new FakeOrderRepository(existingOrder);
        var gateway = new SpyPaymentGateway(PaymentGatewayType.MockSuccess);
        var service = CreateService(repository, gateway);

        var command = new CreateOrderCommand(
            OrderNumber: "ORD-200",
            UserId: "user-1",
            Amount: 300m,
            PaymentGatewayType: PaymentGatewayType.MockSuccess,
            Description: "Original");
        Func<Task> act = () => service.ProcessOrder(command, CancellationToken.None);
        var exceptionAssertion = await act.Should().ThrowAsync<OrderConflictException>();

        exceptionAssertion.Which.OrderNumber.Should().Be("ORD-200");
        gateway.CallCount.Should().Be(0);
        repository.AddCalls.Should().Be(0);
    }

    private static OrderAppService CreateService(FakeOrderRepository repository, SpyPaymentGateway gateway)
    {
        var mapperConfig = new MapperConfiguration(
            config => { config.AddProfile<ApplicationMappingProfile>(); },
            NullLoggerFactory.Instance);

        var resolver = new FakePaymentGatewayResolver(gateway);
        return new OrderAppService(
            paymentGatewayResolver: resolver,
            orderRepository: repository,
            mapper: mapperConfig.CreateMapper(),
            createOrderCommandValidator: new CreateOrderCommandValidator());
    }

    private static CreateOrderCommand CreateValidCommand()
    {
        return new CreateOrderCommand(
            OrderNumber: "ORD-VALID",
            UserId: "user-1",
            Amount: 100m,
            PaymentGatewayType: PaymentGatewayType.MockSuccess,
            Description: "Test");
    }

    private sealed class FakeOrderRepository(params Order[] seededOrders) : IOrderRepository
    {
        private readonly Dictionary<string, Order> _orders = seededOrders.ToDictionary(order => order.OrderNumber);

        public int AddCalls { get; private set; }

        public Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken)
        {
            _orders.TryGetValue(orderNumber, out var order);
            return Task.FromResult(order);
        }

        public Task AddAsync(Order order, CancellationToken cancellationToken)
        {
            AddCalls++;
            _orders[order.OrderNumber] = order;
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakePaymentGatewayResolver(IPaymentGateway gateway) : IPaymentGatewayResolver
    {
        public IPaymentGateway Resolve(PaymentGatewayType gatewayType) => gateway;
    }

    private sealed class SpyPaymentGateway(PaymentGatewayType gatewayType) : IPaymentGateway
    {
        public int CallCount { get; private set; }

        public PaymentGatewayType GatewayType { get; } = gatewayType;

        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new PaymentResult(
                OrderNumber: request.OrderNumber,
                Amount: request.Amount,
                Timestamp: DateTimeOffset.UtcNow,
                ConfirmationNumber: "CONF-MOCK"));
        }
    }
}
