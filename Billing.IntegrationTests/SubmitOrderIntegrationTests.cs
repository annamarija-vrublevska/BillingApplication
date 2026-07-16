using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Billing.Api.Models;
using Billing.Application.Interfaces;
using Billing.Application.Models;
using Billing.IntegrationTests.Infrastructure;
using Billing.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Billing.IntegrationTests;

public sealed class SubmitOrderIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task SubmitOrder_WithValidRequest_ReturnsReceipt()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: "ORD-SUCCESS-1");
        var response = await client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Location?.ToString().Should().EndWith($"/api/orders/{request.OrderNumber}");

        var receipt = await response.Content.ReadFromJsonAsync<PaymentReceiptResponse>(JsonOptions);
        receipt.Should().NotBeNull();
        receipt.OrderNumber.Should().Be(request.OrderNumber);
        receipt.Amount.Should().Be(request.Amount);
        receipt.ConfirmationNumber.Should().NotBeNullOrWhiteSpace();
        receipt.Timestamp.Should().NotBe(default);
        receipt.Status.Should().Be(Domain.Models.OrderStatus.Paid);
        receipt.IsExistingOrder.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitOrder_WithInvalidRequest_ReturnsValidationProblem()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: string.Empty, userId: string.Empty, amount: 0);
        var response = await client.PostAsJsonAsync("/api/orders", request);
        using var json = await ReadJsonAsync(response);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        json.RootElement.GetProperty("title").GetString().Should().Be("One or more validation errors occurred.");
        json.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.TryGetProperty("errors", out var errors).Should().BeTrue();
        errors.TryGetProperty("OrderNumber", out _).Should().BeTrue();
        errors.TryGetProperty("UserId", out _).Should().BeTrue();
        errors.TryGetProperty("Amount", out _).Should().BeTrue();
    }

    [Fact]
    public async Task SubmitOrder_WithUnsupportedGateway_ReturnsBadRequestProblem()
    {
        await using var factory = new BillingApiFactory(services =>
        {
            services.RemoveAll<IPaymentGateway>();
            services.AddScoped<IPaymentGateway>(sp => new ControlledPaymentGateway(
                PaymentGatewayType.MockSuccess,
                sp.GetRequiredService<TestPaymentGatewayController>()));
        });
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: "ORD-UNSUPPORTED-1", gatewayType: PaymentGatewayType.MockFailure);
        var response = await client.PostAsJsonAsync("/api/orders", request);
        using var json = await ReadJsonAsync(response);

        AssertProblem(response, json, HttpStatusCode.BadRequest, "/problems/payment-gateway-not-found", "Payment gateway not found");
    }

    [Fact]
    public async Task SubmitOrder_WithDeclinedPayment_ReturnsUnprocessableEntityProblem()
    {
        await using var factory = new BillingApiFactory();
        factory.GatewayController.SetBehavior(PaymentGatewayType.MockFailure, TestPaymentGatewayBehavior.Declined);
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: "ORD-DECLINED-1", gatewayType: PaymentGatewayType.MockFailure);
        var response = await client.PostAsJsonAsync("/api/orders", request);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        AssertProblem(response, json, HttpStatusCode.UnprocessableEntity, "/problems/payment-failed", "Payment failed");
        body.Should().NotContain("System.");
        body.ToLowerInvariant().Should().NotContain("stack");
    }

    [Fact]
    public async Task SubmitOrder_WithSameRequestTwice_ProcessesPaymentOnce()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: "ORD-IDEMP-1");
        var firstResponse = await client.PostAsJsonAsync("/api/orders", request);
        var secondResponse = await client.PostAsJsonAsync("/api/orders", request);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var first = await firstResponse.Content.ReadFromJsonAsync<PaymentReceiptResponse>(JsonOptions);
        var second = await secondResponse.Content.ReadFromJsonAsync<PaymentReceiptResponse>(JsonOptions);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first.ConfirmationNumber.Should().Be(second!.ConfirmationNumber);
        first.Timestamp.Should().Be(second.Timestamp);
        factory.GatewayController.GetCallCount(PaymentGatewayType.MockSuccess).Should().Be(1);
        (await factory.CountOrdersAsync(request.OrderNumber)).Should().Be(1);
    }

    [Fact]
    public async Task SubmitOrder_WithConflictingDuplicate_ReturnsConflict()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        var firstRequest = CreateValidOrderRequest(orderNumber: "ORD-CONFLICT-2", amount: 100m);
        var conflictingRequest = CreateValidOrderRequest(orderNumber: "ORD-CONFLICT-2", amount: 200m);

        var firstResponse = await client.PostAsJsonAsync("/api/orders", firstRequest);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await client.PostAsJsonAsync("/api/orders", conflictingRequest);
        using var json = await ReadJsonAsync(secondResponse);

        AssertProblem(secondResponse, json, HttpStatusCode.Conflict, "/problems/order-conflict", "Order conflict");
        factory.GatewayController.GetCallCount(PaymentGatewayType.MockSuccess).Should().Be(1);
        (await factory.CountOrdersAsync(firstRequest.OrderNumber)).Should().Be(1);
        (await factory.GetOrderAmountAsync(firstRequest.OrderNumber)).Should().Be(100m);
    }

    [Fact]
    public async Task SubmitOrder_WithConcurrentIdenticalRequests_ProcessesPaymentOnce()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        factory.GatewayController.BlockFirstCall(PaymentGatewayType.MockSuccess);
        var request = CreateValidOrderRequest(orderNumber: "ORD-CONC-IDEMP-1");

        var firstTask = client.PostAsJsonAsync("/api/orders", request);
        await factory.GatewayController.WaitForFirstCallAsync(PaymentGatewayType.MockSuccess, TimeSpan.FromSeconds(5));

        var secondTask = client.PostAsJsonAsync("/api/orders", request);
        factory.GatewayController.Release(PaymentGatewayType.MockSuccess);

        await Task.WhenAll(firstTask, secondTask);

        var first = await firstTask;
        var second = await secondTask;

        factory.GatewayController.GetCallCount(PaymentGatewayType.MockSuccess).Should().Be(1);
        (await factory.CountOrdersAsync(request.OrderNumber)).Should().Be(1);

        var statuses = new[] { first.StatusCode, second.StatusCode };
        statuses.Should().OnlyContain(code =>
            code == HttpStatusCode.Created
            || code == HttpStatusCode.OK
            || code == HttpStatusCode.Conflict);
        statuses.Should().Contain(code => code == HttpStatusCode.Created || code == HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubmitOrder_WithConcurrentConflictingRequests_ReturnsOneConflictAndProcessesOnce()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        factory.GatewayController.BlockFirstCall(PaymentGatewayType.MockSuccess);
        var winningCandidate = CreateValidOrderRequest(orderNumber: "ORD-CONC-CONFLICT-1", amount: 150m);
        var conflictingCandidate = CreateValidOrderRequest(orderNumber: "ORD-CONC-CONFLICT-1", amount: 333m);

        var firstTask = client.PostAsJsonAsync("/api/orders", winningCandidate);
        await factory.GatewayController.WaitForFirstCallAsync(PaymentGatewayType.MockSuccess, TimeSpan.FromSeconds(5));
        var secondTask = client.PostAsJsonAsync("/api/orders", conflictingCandidate);

        factory.GatewayController.Release(PaymentGatewayType.MockSuccess);
        await Task.WhenAll(firstTask, secondTask);

        var first = await firstTask;
        var second = await secondTask;

        var responses = new[] { first, second };
        responses.Count(r => r.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK).Should().Be(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        factory.GatewayController.GetCallCount(PaymentGatewayType.MockSuccess).Should().Be(1);
        (await factory.CountOrdersAsync(winningCandidate.OrderNumber)).Should().Be(1);

        var persistedAmount = await factory.GetOrderAmountAsync(winningCandidate.OrderNumber);
        persistedAmount.Should().BeOneOf(winningCandidate.Amount, conflictingCandidate.Amount);
    }

    [Fact]
    public async Task SubmitOrder_WithUnexpectedException_ReturnsGenericInternalServerErrorProblem()
    {
        await using var factory = new BillingApiFactory(services =>
        {
            services.RemoveAll<IOrderAppService>();
            services.AddScoped<IOrderAppService, ThrowingOrderAppService>();
        });
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: "ORD-UNEXPECTED-1");
        var response = await client.PostAsJsonAsync("/api/orders", request);
        var body = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);

        AssertProblem(response, json, HttpStatusCode.InternalServerError, "/problems/internal-server-error", "Internal server error");
        json.RootElement.GetProperty("detail").GetString()
            .Should().Be("An unexpected error occurred while processing the request.");
        body.Should().NotContain("System.");
        body.ToLowerInvariant().Should().NotContain("sqlite");
        body.ToLowerInvariant().Should().NotContain("stack");
    }

    [Fact]
    public async Task GetOrder_WithExistingOrder_ReturnsOrderResponse()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        var request = CreateValidOrderRequest(orderNumber: "ORD-GET-1", userId: "user-get-1");
        var createResponse = await client.PostAsJsonAsync("/api/orders", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await client.GetAsync($"/api/orders/{request.OrderNumber}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        getResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var order = await getResponse.Content.ReadFromJsonAsync<OrderResponse>(JsonOptions);
        order.Should().NotBeNull();
        order!.OrderNumber.Should().Be(request.OrderNumber);
        order.UserId.Should().Be(request.UserId);
        order.Amount.Should().Be(request.Amount);
        order.PaymentGateway.Should().Be(request.PaymentGatewayType);
        order.Status.Should().Be(Domain.Models.OrderStatus.Paid);
        order.ConfirmationNumber.Should().NotBeNullOrWhiteSpace();
        order.CreatedAt.Should().NotBe(default);
        order.ProcessedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetOrder_WithMissingOrder_ReturnsNotFoundProblem()
    {
        await using var factory = new BillingApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/orders/ORD-NOT-FOUND-1");
        using var json = await ReadJsonAsync(response);

        AssertProblem(
            response,
            json,
            HttpStatusCode.NotFound,
            "/problems/order-not-found",
            "Resource not found",
            "/api/orders/ORD-NOT-FOUND-1");
    }

    private static CreateOrderRequest CreateValidOrderRequest(
        string? orderNumber = null,
        string? userId = null,
        decimal? amount = null,
        PaymentGatewayType? gatewayType = null,
        string? description = "Integration test order")
    {
        return new CreateOrderRequest(
            OrderNumber: orderNumber ?? $"ORD-{Guid.NewGuid():N}",
            UserId: userId ?? "user-1",
            Amount: amount ?? 99.99m,
            PaymentGatewayType: gatewayType ?? PaymentGatewayType.MockSuccess,
            Description: description);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }

    private static void AssertProblem(
        HttpResponseMessage response,
        JsonDocument json,
        HttpStatusCode expectedStatusCode,
        string expectedType,
        string expectedTitle,
        string expectedInstance = "/api/orders")
    {
        response.StatusCode.Should().Be(expectedStatusCode);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        json.RootElement.GetProperty("type").GetString().Should().Be(expectedType);
        json.RootElement.GetProperty("title").GetString().Should().Be(expectedTitle);
        json.RootElement.GetProperty("status").GetInt32().Should().Be((int)expectedStatusCode);
        json.RootElement.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        json.RootElement.GetProperty("instance").GetString().Should().Be(expectedInstance);
    }

    private sealed class ThrowingOrderAppService : IOrderAppService
    {
        public Task<CreateOrderResult> ProcessOrderAsync(CreateOrderCommand command, CancellationToken cancellationToken)
        {
            throw new Exception("Unexpected test exception message with internal details.");
        }

        public Task<GetOrderResult> GetOrderAsync(string orderNumber, CancellationToken cancellationToken)
        {
            throw new Exception("Unexpected test exception message with internal details.");
        }
    }
}
