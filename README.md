# Billing API

Billing API is a small backend coding assignment that demonstrates a payment processing workflow.

The solution focuses on:

- order processing
- idempotency
- multiple payment gateway behaviors
- standardized error responses
- automated tests (unit + integration)

It is intentionally scoped as a compact sample, not a production payment platform.

---

# Features

Implemented features in the current codebase:

- Create order and process payment (`POST /api/orders`)
- Get order by order number (`GET /api/orders/{orderNumber}`)
- Idempotency using `OrderNumber` as the idempotency key
- Conflict detection for same `OrderNumber` with different payload
- Basic concurrent request protection using:
  - unique DB index on `OrderNumber`
  - conflict translation from persistence exceptions
- Multiple payment gateway implementations:
  - `MockSuccess`
  - `MockFailure`
  - `MockRetry`
- Retry policy for transient payment timeouts
- FluentValidation request validation
- Standardized `ProblemDetails` / `ValidationProblemDetails` responses
- Built-in ASP.NET Core logging (`Microsoft.Extensions.Logging`)
- SQLite persistence via EF Core
- Swagger/OpenAPI documentation (Swashbuckle)
- Integration tests for API behavior
- Unit tests for application service behavior
- Single-page demo UI (vanilla HTML/CSS/JS) served by the API

---

# Architecture

The solution is split into focused projects:

## `Billing.Api`

- HTTP layer (controllers, request/response models)
- API mapping profile
- exception handlers that produce standardized ProblemDetails responses
- DI composition root
- static demo UI under `wwwroot`

## `Billing.Application`

- application service orchestration (`OrderAppService`)
- payment gateway resolution
- application models/commands/results
- input validation rules
- application-specific exceptions
- interfaces for repository and gateways

## `Billing.Domain`

- core domain entities and enums
- order state transitions (`Pending`, `Processing`, `Paid`, `Failed`)
- domain invariants (constructor and state mutation guards)

## `Billing.Infrastructure`

- EF Core DbContext and mappings
- SQLite repository implementation
- payment gateway implementations
- infrastructure DI extension

## `Billing.Tests`

- unit tests for `OrderAppService`
- idempotency, validation, retry, and failure behavior at service level

## `Billing.IntegrationTests`

- end-to-end API tests via `WebApplicationFactory`
- controlled test payment gateway behaviors
- coverage for success/failure/idempotency/conflict/retry/concurrency/problem responses

---

# Technologies

Technologies currently used:

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- SQLite
- AutoMapper
- FluentValidation
- Swashbuckle (Swagger/OpenAPI)
- xUnit
- FluentAssertions

No frontend framework is used; demo UI is plain HTML/CSS/JavaScript.

---

# Project Structure

```text
BillingApplication/
├─ Billing.Api/
│  ├─ Controllers/
│  ├─ ExceptionHandlers/
│  ├─ Mapping/
│  ├─ Models/
│  ├─ Properties/
│  ├─ wwwroot/
│  │  ├─ index.html
│  │  └─ app.js
│  └─ Program.cs
├─ Billing.Application/
│  ├─ Exceptions/
│  ├─ Interfaces/
│  ├─ Mapping/
│  ├─ Models/
│  ├─ Services/
│  └─ Validation/
├─ Billing.Domain/
│  └─ Models/
├─ Billing.Infrastructure/
│  ├─ PaymentGateways/
│  ├─ Persistence/
│  │  ├─ Configurations/
│  │  ├─ Migrations/
│  │  └─ Repositories/
│  └─ DependencyInjection.cs
├─ Billing.Tests/
├─ Billing.IntegrationTests/
└─ BillingApplication.slnx
```

---

# Running the Application

## 1. Restore packages

```bash
dotnet restore
```

## 2. Run the API

From repository root:

```bash
dotnet run --project .\Billing.Api\Billing.Api.csproj --launch-profile http
```

Default HTTP URL from launch settings:

- `http://localhost:5023`

## 3. Open Swagger

- `http://localhost:5023/swagger`

## 4. Database migrations

The application runs `Database.Migrate()` on startup, so schema updates are applied automatically when the API starts.
The local SQLite database file is generated automatically on startup and is not committed to source control.

If you prefer explicit migration execution (when EF CLI tools are installed), run:

```bash
dotnet ef database update --project .\Billing.Infrastructure --startup-project .\Billing.Api
```

## 5. Run with Docker

Build image:

```bash
docker build -t billing-api .
```

Run container:

```bash
docker run --rm -p 8080:8080 billing-api
```

Swagger in container run:

- `http://localhost:8080/swagger`

---

# API

## `POST /api/orders`

- Creates/processes an order payment
- Success response body (`PaymentReceiptResponse`) includes:
  - `orderNumber`
  - `amount`
  - `timestamp`
  - `status`
  - `confirmationNumber`
  - `failureReason`
- Returns:
  - `201 Created` for newly processed order
  - `200 OK` for idempotent replay (same payload, same `OrderNumber`)
  - `400` / `409` / `422` / `500` with ProblemDetails for error cases

## `GET /api/orders/{orderNumber}`

- Returns current stored order state and related details
- Returns `404` ProblemDetails when order does not exist

---

# Payment Gateways

Implemented gateway types:

## `MockSuccess`

- Simulates successful payment processing
- Returns a generated confirmation number

## `MockFailure`

- Simulates non-timeout payment failure
- Throws an exception that results in `422 Payment failed`

## `MockRetry`

- Simulates transient timeout failures before success
- Used to demonstrate retry policy behavior for timeout exceptions

---

# Idempotency

`OrderNumber` is used as the idempotency key.

Behavior:

1. First request with a new `OrderNumber`
   - order is created and processed
2. Repeated request with same `OrderNumber` and equivalent payload
   - existing result is returned (`200 OK`)
   - payment is not processed again
   - replay is indicated by HTTP status (`200`), not a dedicated response flag
3. Repeated request with same `OrderNumber` but different payload
   - conflict response (`409`)

---

# Error Handling

The API uses centralized exception handlers and always returns standardized payloads for failures.

Implemented response types:

- `ValidationProblemDetails`
  - for FluentValidation errors (`400 Bad Request`)
  - includes grouped field errors in `errors`
- `ProblemDetails`
  - for known application exceptions (`400`, `404`, `409`, `422`)
  - for unhandled exceptions (`500`)

Each error payload includes a `traceId` extension for request correlation.

---

# Retry Policy

Retry behavior is implemented in `OrderAppService.ProcessPaymentWithRetryAsync`.

Current behavior:

- Retries only `TimeoutException`
- Max attempts: `3`
- Delay between retries: `200 ms`
- If retries are exhausted, payment is marked failed and returned as `PaymentFailedException` (`422`)

Intentionally out of scope for this assignment:

- external retry libraries
- advanced backoff/jitter strategies
- circuit breakers
- distributed resilience policies

---

# Logging

Logging uses the built-in `Microsoft.Extensions.Logging` infrastructure.

Current logging includes:

- order processing lifecycle events (created, replay, completed, failed)
- transient retry warnings
- global unhandled exception logging in API exception handler

No external logging stack is configured.

---

# Testing

## Unit tests (`Billing.Tests`)

`OrderAppServiceTests` covers:

- successful processing
- retry success after transient timeout
- retry exhaustion and failure
- command validation errors
- idempotent replay behavior
- conflict behavior for duplicate order numbers with different payloads

## Integration tests (`Billing.IntegrationTests`)

`SubmitOrderIntegrationTests` covers:

- successful order submission
- validation error responses
- unsupported gateway response
- payment failure response
- transient timeout retry success
- persistent timeout failure after retries
- idempotent repeated request behavior
- conflicting duplicate behavior
- concurrent identical requests behavior
- concurrent conflicting requests behavior
- generic internal server error response shape
- get existing order
- get missing order

---

# Demo UI

A small single-page demo UI is available at:

- `http://localhost:5023/`

It submits `POST /api/orders` requests and displays:

- success details
- ProblemDetails error details
- grouped validation errors

The UI is intentionally minimal and used only for API demonstration.
