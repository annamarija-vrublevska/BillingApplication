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

        var existingOrder = await orderRepository.GetByOrderNumberAsync(command.OrderNumber, cancellationToken);
        if (existingOrder is not null)
        {
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

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = command.OrderNumber,
            UserId = command.UserId,
            Amount = command.Amount,
            Description = command.Description,
            PaymentGatewayId = (int)command.PaymentGatewayType,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await orderRepository.AddAsync(order, cancellationToken);
        await orderRepository.SaveChangesAsync(cancellationToken);

        var gateway = paymentGatewayResolver.Resolve(command.PaymentGatewayType);
        var paymentRequest = mapper.Map<PaymentRequest>(command);

        order.Status = OrderStatus.Processing;
        await orderRepository.SaveChangesAsync(cancellationToken);

        try
        {
            var paymentResult = await gateway.ProcessPaymentAsync(paymentRequest, cancellationToken);

            order.Status = OrderStatus.Paid;
            order.ConfirmationNumber = paymentResult.ConfirmationNumber;
            order.ProcessedAt = DateTime.UtcNow;
            order.FailureReason = null;

            await orderRepository.SaveChangesAsync(cancellationToken);

            return mapper.Map<CreateOrderResult>(paymentResult);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            order.Status = OrderStatus.Failed;
            order.FailureReason = ex.Message;
            order.ProcessedAt = DateTime.UtcNow;

            await orderRepository.SaveChangesAsync(cancellationToken);
            throw;
        }
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