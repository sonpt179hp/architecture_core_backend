---
name: resilience
description: >
  Resilience patterns for .NET 8 LTS applications. Covers Polly v8 pipelines
  (retry, circuit breaker, timeout, fallback, hedging), MassTransit consumer
  idempotency, Dead Letter Queue (DLQ) configuration, and concurrent update handling
  via DbUpdateConcurrencyException.
  Load this skill when implementing retry logic, circuit breakers, handling
  transient failures, or when the user mentions "Polly", "resilience", "retry",
  "circuit breaker", "timeout", "fallback", "rate limit", "hedging",
  "idempotent consumer", "DLQ", "dead letter", "DbUpdateConcurrencyException",
  "transient fault", "HttpClient resilience", or "resilience pipeline".
---

# Resilience (.NET 8)

## Core Principles

1. **Polly v8 resilience pipelines, not v7 policies** — Polly v8 replaced `Policy` with `ResiliencePipeline`. Never use `PolicyBuilder`, `Policy.Handle<>()`, or `ISyncPolicy`. The new API is composable, type-safe, and integrates natively with `IHttpClientFactory`.
2. **Configure via `AddResilienceHandler`, not manual wrapping** — For HTTP calls, use `Microsoft.Extensions.Http.Resilience` to attach pipelines directly to `HttpClient` via DI. No per-call `ExecuteAsync` wrapping.
3. **Compose strategies, don't nest them** — A single `ResiliencePipeline` chains retry + circuit breaker + timeout. Strategies execute outer-to-inner (first added = outermost). No nested try/catch orchestration.
4. **Always set timeouts** — Every external call needs a timeout. `AddTimeout()` innermost = per-attempt; outermost = total elapsed. Never omit either.
5. **MassTransit consumers must be idempotent** — Check `MessageId` in a `ProcessedMessages` table before executing side-effects. Write the `MessageId` and side-effects in the same transaction. Skip if already processed.
6. **DLQ for every consumer** — Use `UseDeadLetterQueueDeadLetterTransport()` in `ConsumerDefinition`. Failed messages after max retries go to DLQ, never block the queue.

## Patterns

### HTTP Client Resilience (Recommended Default)

```csharp
// Program.cs — covers 90% of use cases
builder.Services.AddHttpClient<IPaymentGateway, PaymentGatewayClient>(client =>
{
    client.BaseAddress = new Uri("https://api.payments.example.com");
})
.AddStandardResilienceHandler();
// Configured automatically:
// - Retry: 3 attempts, exponential backoff + jitter
// - Circuit breaker: 10% failure ratio / 30s sampling, 30s break
// - Attempt timeout: 10s | Total timeout: 30s
```

### Custom HTTP Resilience

```csharp
builder.Services.AddHttpClient<ICatalogService, CatalogServiceClient>()
.AddResilienceHandler("catalog", pipeline =>
{
    pipeline.AddTimeout(TimeSpan.FromSeconds(15)); // total — outermost

    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        Delay = TimeSpan.FromMilliseconds(500),
        ShouldHandle = static args => ValueTask.FromResult(
            args.Outcome.Result?.StatusCode is HttpStatusCode.RequestTimeout
                or HttpStatusCode.TooManyRequests
                or HttpStatusCode.ServiceUnavailable
                || args.Outcome.Exception is HttpRequestException)
    });

    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(10),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    pipeline.AddTimeout(TimeSpan.FromSeconds(5)); // per attempt — innermost
});
```

**Why**: Named resilience handlers let you tune per-service. Order matters: total timeout → retry → circuit breaker → attempt timeout.

### Non-HTTP Resilience Pipeline

```csharp
// For database calls, message queues, or any non-HTTP operation
builder.Services.AddResiliencePipeline("database", pipeline =>
{
    pipeline
        .AddRetry(new RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            BackoffType = DelayBackoffType.Exponential,
            Delay = TimeSpan.FromMilliseconds(200),
            ShouldHandle = new PredicateBuilder()
                .Handle<TimeoutException>()
                .Handle<InvalidOperationException>(ex =>
                    ex.Message.Contains("deadlock", StringComparison.OrdinalIgnoreCase))
        })
        .AddTimeout(TimeSpan.FromSeconds(10));
});

// Inject and use
public sealed class OrderRepository(
    AppDbContext db,
    [FromKeyedServices("database")] ResiliencePipeline pipeline)
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct) =>
        await pipeline.ExecuteAsync(
            async token => await db.Orders.FindAsync([id], token), ct);
}
```

### MassTransit ConsumerDefinition with Retry + DLQ

```csharp
public class OrderCreatedConsumerDefinition : ConsumerDefinition<OrderCreatedConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<OrderCreatedConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(r =>
        {
            r.Exponential(
                retryLimit: 5,
                minInterval: TimeSpan.FromSeconds(1),
                maxInterval: TimeSpan.FromMinutes(5),
                intervalDelta: TimeSpan.FromSeconds(2));
            r.Ignore<BusinessException>();    // no retry on domain errors
            r.Ignore<ValidationException>(); // no retry on validation
        });

        endpointConfigurator.UseDeadLetterQueueDeadLetterTransport(); // DLQ after max retries
        endpointConfigurator.UseDeadLetterQueueFaultTransport();
    }
}
```

**Why**: `Ignore<T>()` prevents retrying business errors that will never succeed. `UseDeadLetterQueueDeadLetterTransport()` sends exhausted messages to DLQ instead of dropping them.

### Idempotent Consumer

```csharp
public class OrderCreatedConsumer(AppDbContext db) : IConsumer<OrderCreated>
{
    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var messageId = context.MessageId?.ToString()
            ?? throw new InvalidOperationException("MessageId is required.");

        // Idempotency check — skip if already processed
        if (await db.ProcessedMessages.AnyAsync(m => m.MessageId == messageId))
            return;

        // Side-effects + idempotency record in the same transaction
        await using var tx = await db.Database.BeginTransactionAsync();

        db.Notifications.Add(new OrderNotification(context.Message.OrderId));
        db.ProcessedMessages.Add(new ProcessedMessage(messageId, DateTimeOffset.UtcNow));
        await db.SaveChangesAsync();

        await tx.CommitAsync();
    }
}
```

**Why**: Writing `MessageId` in the same transaction as side-effects guarantees exactly-once processing even under retries or broker redelivery.

### Concurrent Update Handling (Optimistic Concurrency)

When `DbUpdateConcurrencyException` occurs, reload the entity to get the current DB state, then return 409 and let the client retry with fresh data.

```csharp
public async Task<IResult> UpdateOrderAsync(Guid id, UpdateOrderRequest req, AppDbContext db)
{
    try
    {
        var order = await db.Orders.FindAsync(id);
        if (order is null) return Results.NotFound();

        order.Update(req.Status);
        await db.SaveChangesAsync();
        return Results.Ok();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Reload from DB to get the latest state before any client retry
        await ex.Entries.First().ReloadAsync();
        return Results.Conflict(new ProblemDetails
        {
            Title = "Concurrent update conflict",
            Detail = "The resource was modified by another request. Please retry.",
            Status = 409
        });
    }
}
```

### Hedging (Parallel Requests for Low-Latency Reads)

```csharp
builder.Services.AddHttpClient<ISearchService, SearchServiceClient>()
    .AddResilienceHandler("search-hedging", pipeline =>
    {
        pipeline.AddHedging(new HttpHedgingStrategyOptions
        {
            MaxHedgedAttempts = 2,
            Delay = TimeSpan.FromMilliseconds(500) // send parallel request after 500ms
        });
        pipeline.AddTimeout(TimeSpan.FromSeconds(3));
    });
```

**Why**: Hedging sends a parallel request if the first hasn't responded within the delay. Use for latency-sensitive reads where duplicate work is tolerable.

### Telemetry Integration

```csharp
// Polly v8 emits metrics via System.Diagnostics.Metrics automatically
// Wire up OpenTelemetry to capture them
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics.AddMeter("Polly"));

// Dashboard alert on: polly.circuit_breaker.state = Open
```

### Rate Limiting (.NET Built-in)

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromSeconds(60);
        opt.QueueLimit = 0;
    });
    options.OnRejected = async (ctx, ct) =>
    {
        ctx.HttpContext.Response.StatusCode = 429;
        if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            ctx.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        await ctx.HttpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Title = "Too many requests", Status = 429 }, ct);
    };
});
app.UseRateLimiter();
app.MapGet("/api/orders", ListOrders).RequireRateLimiting("fixed");
```

## Anti-patterns

### DON'T Retry Business Errors

```csharp
// BAD — retries DomainException, ValidationException, 404; all will fail again
endpointConfigurator.UseMessageRetry(r => r.Immediate(retryLimit: 5));

// GOOD — only retry transient infrastructure errors; ignore domain/validation errors
endpointConfigurator.UseMessageRetry(r =>
{
    r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(2));
    r.Ignore<BusinessException>();
    r.Ignore<ValidationException>();
});
```

### DON'T Retry DbUpdateConcurrencyException Without Reloading

```csharp
// BAD — retrying with stale entity data will fail all attempts identically
builder.AddRetry(new RetryStrategyOptions
{
    ShouldHandle = new PredicateBuilder().Handle<DbUpdateConcurrencyException>()
    // All retries fail with the same stale RowVersion — wasted attempts
});

// GOOD — reload entity from DB first, then return 409 for the client to retry
catch (DbUpdateConcurrencyException ex)
{
    await ex.Entries.First().ReloadAsync(); // fetch current DB values
    return Results.Conflict(/* let the client retry with latest state */);
}
```

### DON'T Share a Circuit Breaker Instance Across Services

```csharp
// BAD — ServiceA failures trip the breaker and break ServiceB calls too
builder.Services.AddSingleton(new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions())
    .Build()); // one shared instance = one failure domain for all services

// GOOD — named handler per service; each has its own circuit breaker
builder.Services.AddHttpClient<IServiceA, ServiceA>()
    .AddResilienceHandler("service-a", ...);
builder.Services.AddHttpClient<IServiceB, ServiceB>()
    .AddResilienceHandler("service-b", ...);
```

### DON'T Use Polly v7 API

```csharp
// BAD — v7 policy syntax
var policy = Policy.Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, n => TimeSpan.FromSeconds(Math.Pow(2, n)));

// GOOD — v8 pipeline via DI
builder.Services.AddHttpClient<IService, Service>()
    .AddStandardResilienceHandler();
```

## Decision Guide

| Scenario | Strategy | Notes |
|----------|----------|-------|
| HTTP calls to external APIs | `AddStandardResilienceHandler()` | Defaults cover 90% of cases |
| HTTP with custom thresholds | `AddResilienceHandler("name", ...)` | Named handler per service |
| Database / EF Core calls | `AddResiliencePipeline("db", ...)` | Retry deadlock/timeout only |
| MassTransit consumer retry | `ConsumerDefinition` + `UseMessageRetry` | Exponential, ignore domain errors |
| Failed messages after max retries | `UseDeadLetterQueueDeadLetterTransport()` | DLQ, not dropped |
| Idempotent consumer | Check `MessageId` in `ProcessedMessages` | Write record in same transaction |
| Concurrent DB update conflict | `DbUpdateConcurrencyException` → reload → 409 | Client retries with latest state |
| Latency-sensitive reads | `AddHedging(...)` | Parallel request after delay |
| Graceful degradation | `AddFallback(...)` | Return cached / default value |
| API rate limiting | `AddRateLimiter()` + `RequireRateLimiting()` | Fixed, sliding, or token bucket |
| Non-idempotent writes | Idempotency key header, or fail fast | No retry without idempotency key |
