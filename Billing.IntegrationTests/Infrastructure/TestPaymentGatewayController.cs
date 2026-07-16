using Billing.Application.Models;
using Billing.Domain.Models;

namespace Billing.IntegrationTests.Infrastructure;

public sealed class TestPaymentGatewayController
{
    private readonly Dictionary<PaymentGatewayType, GatewayState> _states = new()
    {
        [PaymentGatewayType.MockSuccess] = new GatewayState(),
        [PaymentGatewayType.MockFailure] = new GatewayState()
    };

    public void SetBehavior(PaymentGatewayType gatewayType, TestPaymentGatewayBehavior behavior)
    {
        _states[gatewayType].Behavior = behavior;
    }

    public int GetCallCount(PaymentGatewayType gatewayType)
    {
        return _states[gatewayType].CallCount;
    }

    public void BlockFirstCall(PaymentGatewayType gatewayType)
    {
        var state = _states[gatewayType];
        state.BlockFirstCall = true;
        state.FirstCallStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        state.ReleaseBlockedCall = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public async Task WaitForFirstCallAsync(PaymentGatewayType gatewayType, TimeSpan timeout)
    {
        var state = _states[gatewayType];
        if (state.FirstCallStarted is null)
        {
            throw new InvalidOperationException("First-call blocking is not configured.");
        }

        using var timeoutCts = new CancellationTokenSource(timeout);
        await state.FirstCallStarted.Task.WaitAsync(timeoutCts.Token);
    }

    public void Release(PaymentGatewayType gatewayType)
    {
        var state = _states[gatewayType];
        state.ReleaseBlockedCall?.TrySetResult(true);
    }

    internal async Task<PaymentResult> ProcessAsync(
        PaymentGatewayType gatewayType,
        PaymentRequest request,
        CancellationToken cancellationToken)
    {
        var state = _states[gatewayType];
        state.CallCount++;

        if (state is { BlockFirstCall: true, HasBlocked: false })
        {
            state.HasBlocked = true;
            state.FirstCallStarted?.TrySetResult(true);
            if (state.ReleaseBlockedCall is not null)
            {
                await state.ReleaseBlockedCall.Task.WaitAsync(cancellationToken);
            }
        }

        return state.Behavior switch
        {
            TestPaymentGatewayBehavior.Success => new PaymentResult(
                OrderNumber: request.OrderNumber,
                Amount: request.Amount,
                Timestamp: DateTimeOffset.UtcNow,
                ConfirmationNumber: $"CONF-{request.OrderNumber}"),
            TestPaymentGatewayBehavior.Declined => throw new InvalidOperationException("Test payment declined."),
            TestPaymentGatewayBehavior.UnexpectedException => throw new Exception("Test unexpected payment exception."),
            _ => throw new InvalidOperationException("Unsupported test gateway behavior.")
        };
    }

    private sealed class GatewayState
    {
        public TestPaymentGatewayBehavior Behavior { get; set; } = TestPaymentGatewayBehavior.Success;
        public int CallCount { get; set; }
        public bool BlockFirstCall { get; set; }
        public bool HasBlocked { get; set; }
        public TaskCompletionSource<bool>? FirstCallStarted { get; set; }
        public TaskCompletionSource<bool>? ReleaseBlockedCall { get; set; }
    }
}
