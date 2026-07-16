using AutoMapper;
using Billing.Application.Exceptions;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.Domain.Models;
using FluentValidation;

namespace Billing.Application.Services;

public class OrderAppService(
    IPaymentGatewayResolver paymentGatewayResolver,
    IOrderRepository orderRepository,
    IMapper mapper,
    IValidator<CreateOrderCommand> createOrderCommandValidator) : IOrderAppService
{
    public async Task<CreateOrderResult> ProcessOrder(CreateOrderCommand command, CancellationToken cancellationToken)
    {
        await createOrderCommandValidator.ValidateAndThrowAsync(command, cancellationToken);

        var idempotentResult = await TryGetIdempotentResultAsync(command, cancellationToken);
        if (idempotentResult is not null)
        {
            return idempotentResult;
        }

        var order = await CreatePendingOrderAsync(command, cancellationToken);

        var gateway = paymentGatewayResolver.Resolve(command.PaymentGatewayType);
        var paymentRequest = mapper.Map<PaymentRequest>(command);

        await MarkOrderAsProcessingAsync(order, cancellationToken);

        try
        {
            var paymentResult = await gateway.ProcessPaymentAsync(paymentRequest, cancellationToken);
            await MarkOrderAsPaidAsync(order, paymentResult.ConfirmationNumber, cancellationToken);

            return mapper.Map<CreateOrderResult>(paymentResult);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await MarkOrderAsFailedAsync(order, ex.Message, cancellationToken);
            throw;
        }
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
                (int)command.PaymentGatewayType,
                command.Description))
        {
            throw new OrderConflictException(command.OrderNumber);
        }

        return CreateResultFromOrder(existingOrder);
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
            paymentGatewayId: (int)command.PaymentGatewayType);

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

    private static CreateOrderResult CreateResultFromOrder(Order order)
    {
        var timestamp = DateTime.SpecifyKind(order.ProcessedAt ?? order.CreatedAt, DateTimeKind.Utc);
        return new CreateOrderResult(
            OrderNumber: order.OrderNumber,
            Amount: order.Amount,
            Timestamp: new DateTimeOffset(timestamp),
            ConfirmationNumber: order.ConfirmationNumber ?? string.Empty);
    }
}