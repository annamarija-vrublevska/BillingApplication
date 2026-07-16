using AutoMapper;
using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Domain.Models;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Billing.Application.Services;

public class OrderAppService(
    IPaymentGatewayResolver paymentGatewayResolver,
    IOrderRepository orderRepository,
    IMapper mapper,
    IValidator<CreateOrderCommand> createOrderCommandValidator,
    ILogger<OrderAppService> logger) : IOrderAppService
{
    public async Task<CreateOrderResult> ProcessOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        await createOrderCommandValidator.ValidateAndThrowAsync(command, cancellationToken);

        var idempotentResult = await TryGetIdempotentResultAsync(command, cancellationToken);
        if (idempotentResult is not null)
        {
            return idempotentResult;
        }

        var order = await CreatePendingOrderAsync(command, cancellationToken);
        logger.LogInformation(
            "Order {OrderNumber} created for processing using gateway {Gateway}.",
            order.OrderNumber,
            order.PaymentGateway);

        IPaymentGateway gateway;
        try
        {
            gateway = paymentGatewayResolver.Resolve(command.PaymentGatewayType);
        }
        catch (PaymentGatewayNotFoundException)
        {
            logger.LogWarning(
                "Payment gateway {Gateway} is not available for order {OrderNumber}.",
                command.PaymentGatewayType,
                command.OrderNumber);
            throw;
        }

        var paymentRequest = mapper.Map<PaymentRequest>(command);

        await MarkOrderAsProcessingAsync(order, cancellationToken);

        PaymentResult paymentResult;
        try
        {
            paymentResult = await ProcessPaymentWithRetryAsync(gateway, paymentRequest, order, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await MarkOrderAsFailedAsync(order, ex.Message, cancellationToken);
            logger.LogWarning(
                "Payment failed for order {OrderNumber} using gateway {Gateway}.",
                order.OrderNumber,
                order.PaymentGateway);
            throw new PaymentFailedException(order.OrderNumber, ex);
        }

        await MarkOrderAsPaidAsync(order, paymentResult.ConfirmationNumber, cancellationToken);
        logger.LogInformation(
            "Payment completed for order {OrderNumber} using gateway {Gateway}.",
            order.OrderNumber,
            order.PaymentGateway);
        return CreateResultFromOrder(order, isExistingOrder: false);
    }

    public async Task<GetOrderResult> GetOrderAsync(string orderNumber, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);

        var order = await orderRepository.GetByOrderNumberAsync(orderNumber, cancellationToken);
        if (order is null)
        {
            throw new OrderNotFoundException(orderNumber);
        }

        var processedAt = order.ProcessedAt.HasValue
            ? new DateTimeOffset(order.ProcessedAt.Value)
            : (DateTimeOffset?)null;

        return new GetOrderResult(
            OrderNumber: order.OrderNumber,
            UserId: order.UserId,
            Amount: order.Amount,
            PaymentGateway: order.PaymentGateway,
            Status: order.Status,
            Description: order.Description,
            CreatedAt: new DateTimeOffset(order.CreatedAt),
            ProcessedAt: processedAt,
            ConfirmationNumber: order.ConfirmationNumber,
            FailureReason: order.FailureReason);
    }

    private async Task<PaymentResult> ProcessPaymentWithRetryAsync(
        IPaymentGateway gateway,
        PaymentRequest paymentRequest,
        Order order,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 3;
        const int retryDelayMilliseconds = 200;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await gateway.ProcessPaymentAsync(
                    paymentRequest,
                    cancellationToken);
            }
            catch (TimeoutException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    ex,
                    "Transient payment failure for order {OrderNumber}. Retrying attempt {NextAttempt}/{MaxAttempts}.",
                    order.OrderNumber,
                    attempt + 1,
                    maxAttempts);

                await Task.Delay(
                    retryDelayMilliseconds,
                    cancellationToken);
            }
        }

        throw new InvalidOperationException(
            "Payment retry loop completed without returning a result.");
    }

    private async Task<CreateOrderResult?> TryGetIdempotentResultAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var existingOrder = await orderRepository.GetByOrderNumberAsync(command.OrderNumber, cancellationToken);
        if (existingOrder is null)
        {
            return null;
        }

        if (!existingOrder.IsEquivalentTo(
                command.UserId,
                command.Amount,
                command.PaymentGatewayType,
                command.Description))
        {
            logger.LogWarning(
                "Order conflict detected for order {OrderNumber}: same idempotency key with different payload.",
                command.OrderNumber);
            throw new OrderConflictException(command.OrderNumber);
        }

        logger.LogInformation(
            "Idempotent replay returned existing receipt for order {OrderNumber}.",
            command.OrderNumber);
        return CreateResultFromOrder(existingOrder, isExistingOrder: true);
    }

    private async Task<Order> CreatePendingOrderAsync(
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = new Order(
            orderNumber: command.OrderNumber,
            userId: command.UserId,
            amount: command.Amount,
            description: command.Description,
            paymentGateway: command.PaymentGatewayType);

        await orderRepository.AddAsync(order, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);
        return order;
    }

    private async Task MarkOrderAsProcessingAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        order.MarkAsProcessing();
        await orderRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkOrderAsPaidAsync(
        Order order,
        string confirmationNumber,
        CancellationToken cancellationToken)
    {
        order.MarkAsPaid(confirmationNumber);
        await orderRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkOrderAsFailedAsync(
        Order order,
        string failureReason,
        CancellationToken cancellationToken)
    {
        order.MarkAsFailed(failureReason);
        await orderRepository.SaveChangesAsync(cancellationToken);
    }

    private static CreateOrderResult CreateResultFromOrder(Order order, bool isExistingOrder)
    {
        return new CreateOrderResult(
            OrderNumber: order.OrderNumber,
            Amount: order.Amount,
            Timestamp: new DateTimeOffset(order.ProcessedAt ?? order.CreatedAt),
            Status: order.Status,
            ConfirmationNumber: order.ConfirmationNumber,
            FailureReason: order.FailureReason,
            IsExistingOrder: isExistingOrder);
    }
}